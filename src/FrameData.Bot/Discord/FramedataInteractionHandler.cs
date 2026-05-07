using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using FrameData.Bot.Api;
using FrameData.Bot.Formatting;
using FrameData.Shared.Contracts;
using FrameData.Shared.Logging;
using Microsoft.Extensions.Logging;

namespace FrameData.Bot.Discord;

public sealed class FramedataInteractionHandler
{
    private readonly SlashCommandInteractionMapper _mapper;
    private readonly IMoveQueryApiClient _apiClient;
    private readonly MoveEmbedResponseFactory _responseFactory;
    private readonly ILogger<FramedataInteractionHandler> _logger;

    public FramedataInteractionHandler(
        SlashCommandInteractionMapper mapper,
        IMoveQueryApiClient apiClient,
        MoveEmbedResponseFactory responseFactory,
        ILogger<FramedataInteractionHandler> logger)
    {
        _mapper = mapper;
        _apiClient = apiClient;
        _responseFactory = responseFactory;
        _logger = logger;
    }

    public Task HandleSocketSlashCommandAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var commandName = ((IApplicationCommandInteractionData)command.Data).Name;
        var options = command.Data.Options.Select(option => new SlashCommandOptionValue(option.Name, option.Value));
        return HandleAsync(commandName, options, new SocketSlashCommandResponder(command), cancellationToken);
    }

    public async Task HandleAsync(
        string commandName,
        IEnumerable<SlashCommandOptionValue> options,
        IDiscordInteractionResponder responder,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();
        var mapped = _mapper.Map(commandName, options);
        if (!mapped.IsValid)
        {
            _logger.LogInformation(
                "Rejected Discord command {CommandName}: {ValidationError}.",
                commandName,
                mapped.Error);
            LogDiscordInteractionRejectedMetric(
                commandName,
                mapped.Error,
                Stopwatch.GetElapsedTime(started));

            await responder.RespondAsync(mapped.Error ?? "Invalid command.");
            return;
        }

        var invocation = mapped.Invocation!;
        _logger.LogInformation(
            "Handling Discord /framedata interaction for character {Character} and move input {MoveInput}.",
            invocation.Character,
            invocation.Move);

        await responder.DeferAsync();

        try
        {
            var result = await _apiClient.QueryMoveAsync(invocation.Character, invocation.Move, cancellationToken);
            var issueContext = new MoveCorrectionIssueContext(invocation.Character, invocation.Move);
            var moveResponse = FormatQueryResult(result.Response, result.Ambiguous, result.Error, issueContext);
            LogMediaAttachment(result.Response, moveResponse.Attachment);

            await responder.FollowupAsync(
                moveResponse.Content,
                moveResponse.Embed,
                moveResponse.IsEphemeral,
                moveResponse.Attachment,
                moveResponse.Components);
            var resultCode = GetInteractionResultCode(result.Response, result.Ambiguous, result.Error);
            _logger.LogInformation(
                "Discord /framedata interaction completed for character {Character} and move input {MoveInput} with result {ResultCode}.",
                invocation.Character,
                invocation.Move,
                resultCode);
            LogDiscordInteractionCompletedMetric(
                commandName,
                invocation.Character,
                invocation.Move,
                resultCode,
                result.Response?.Character,
                result.Response?.MatchedMove,
                result.Response?.MatchedBy,
                result.Ambiguous?.Candidates.Count ?? 0,
                result.Error?.Code,
                moveResponse.IsEphemeral,
                moveResponse.Attachment is not null,
                Stopwatch.GetElapsedTime(started));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle Discord /framedata interaction.");
            LogDiscordInteractionFailedMetric(
                commandName,
                invocation.Character,
                invocation.Move,
                exception.GetType().Name,
                Stopwatch.GetElapsedTime(started));
            await responder.FollowupAsync("Unable to query frame data right now. Try again shortly.");
        }
    }

    private static string GetInteractionResultCode(
        MoveQueryResponse? response,
        MoveAmbiguousResponse? ambiguous,
        ErrorResponse? error)
    {
        if (response is not null)
        {
            return "ok";
        }

        if (ambiguous is not null)
        {
            return "ambiguous_move";
        }

        return error?.Code ?? "error";
    }

    private void LogDiscordInteractionCompletedMetric(
        string commandName,
        string character,
        string moveInput,
        string resultCode,
        string? matchedCharacter,
        string? matchedMove,
        string? matchedBy,
        int candidateCount,
        string? errorCode,
        bool isEphemeral,
        bool hasAttachment,
        TimeSpan elapsed)
    {
        _logger.LogInformation(
            "Metric {MetricName}: Discord /framedata interaction completed. CommandName={CommandName}; ResultCode={ResultCode}; Character={Character}; MoveInput={MoveInput}; MatchedCharacter={MatchedCharacter}; MatchedMove={MatchedMove}; MatchedBy={MatchedBy}; CandidateCount={CandidateCount}; ErrorCode={ErrorCode}; IsEphemeral={IsEphemeral}; HasAttachment={HasAttachment}; ElapsedMs={ElapsedMs:0.000}.",
            FrameDataMetricNames.DiscordFramedataInteractionCompleted,
            commandName,
            resultCode,
            character,
            moveInput,
            matchedCharacter,
            matchedMove,
            matchedBy,
            candidateCount,
            errorCode,
            isEphemeral,
            hasAttachment,
            elapsed.TotalMilliseconds);
    }

    private void LogDiscordInteractionRejectedMetric(
        string commandName,
        string? validationError,
        TimeSpan elapsed)
    {
        _logger.LogInformation(
            "Metric {MetricName}: Discord command rejected. CommandName={CommandName}; ResultCode={ResultCode}; ValidationError={ValidationError}; ElapsedMs={ElapsedMs:0.000}.",
            FrameDataMetricNames.DiscordFramedataInteractionRejected,
            commandName,
            "validation_error",
            validationError,
            elapsed.TotalMilliseconds);
    }

    private void LogDiscordInteractionFailedMetric(
        string commandName,
        string character,
        string moveInput,
        string exceptionType,
        TimeSpan elapsed)
    {
        _logger.LogInformation(
            "Metric {MetricName}: Discord /framedata interaction failed. CommandName={CommandName}; ResultCode={ResultCode}; Character={Character}; MoveInput={MoveInput}; ExceptionType={ExceptionType}; ElapsedMs={ElapsedMs:0.000}.",
            FrameDataMetricNames.DiscordFramedataInteractionFailed,
            commandName,
            "exception",
            character,
            moveInput,
            exceptionType,
            elapsed.TotalMilliseconds);
    }

    private DiscordMoveResponse FormatQueryResult(
        MoveQueryResponse? response,
        MoveAmbiguousResponse? ambiguous,
        ErrorResponse? error,
        MoveCorrectionIssueContext issueContext)
    {
        if (response is not null)
        {
            return _responseFactory.Create(response, issueContext);
        }

        if (ambiguous is not null)
        {
            return _responseFactory.Create(ambiguous, issueContext);
        }

        return error is null
            ? _responseFactory.CreateFallbackError(issueContext)
            : _responseFactory.Create(error, issueContext);
    }

    private void LogMediaAttachment(MoveQueryResponse? response, DiscordMoveAttachment? attachment)
    {
        if (response?.Media is null)
        {
            return;
        }

        if (attachment is null)
        {
            _logger.LogWarning(
                "Move query response for {Character}/{Move} included media metadata but no Discord attachment was prepared. RepresentativeFrameImageUrl={RepresentativeFrameImageUrl}; CaptureStatus={CaptureStatus}.",
                response.Character,
                response.MatchedMove,
                response.Media.RepresentativeFrameImageUrl,
                response.Media.CaptureStatus);
            return;
        }

        if (File.Exists(attachment.FilePath))
        {
            _logger.LogInformation(
                "Prepared Discord media attachment for {Character}/{Move}. FilePath={FilePath}; FileName={FileName}; RepresentativeFrameImageUrl={RepresentativeFrameImageUrl}; CaptureStatus={CaptureStatus}.",
                response.Character,
                response.MatchedMove,
                attachment.FilePath,
                attachment.FileName,
                response.Media.RepresentativeFrameImageUrl,
                response.Media.CaptureStatus);
            return;
        }

        _logger.LogWarning(
            "Discord media attachment for {Character}/{Move} was prepared but the file does not exist at {FilePath}. RepresentativeFrameImageUrl={RepresentativeFrameImageUrl}; CaptureStatus={CaptureStatus}. Check the bot dataset volume mount and FRAMEDATA_ACTIVE_DATASET_PATH.",
            response.Character,
            response.MatchedMove,
            attachment.FilePath,
            response.Media.RepresentativeFrameImageUrl,
            response.Media.CaptureStatus);
    }
}
