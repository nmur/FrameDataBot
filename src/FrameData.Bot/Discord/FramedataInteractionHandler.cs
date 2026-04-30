using Discord;
using Discord.WebSocket;
using FrameData.Bot.Api;
using FrameData.Bot.Formatting;
using FrameData.Shared.Contracts;
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
        var mapped = _mapper.Map(commandName, options);
        if (!mapped.IsValid)
        {
            _logger.LogInformation(
                "Rejected Discord command {CommandName}: {ValidationError}.",
                commandName,
                mapped.Error);

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
            var moveResponse = FormatQueryResult(result.Response, result.Ambiguous, result.Error);
            LogMediaAttachment(result.Response, moveResponse.Attachment);
            _logger.LogInformation(
                "Discord /framedata interaction completed for character {Character} and move input {MoveInput} with result {ResultCode}.",
                invocation.Character,
                invocation.Move,
                result.Response is not null ? "ok" : result.Ambiguous is not null ? "ambiguous" : result.Error?.Code ?? "error");

            await responder.FollowupAsync(
                moveResponse.Content,
                moveResponse.Embed,
                moveResponse.IsEphemeral,
                moveResponse.Attachment);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle Discord /framedata interaction.");
            await responder.FollowupAsync("Unable to query frame data right now. Try again shortly.");
        }
    }

    private DiscordMoveResponse FormatQueryResult(MoveQueryResponse? response, MoveAmbiguousResponse? ambiguous, ErrorResponse? error)
    {
        if (response is not null)
        {
            return _responseFactory.Create(response);
        }

        if (ambiguous is not null)
        {
            return _responseFactory.Create(ambiguous);
        }

        return error is null ? _responseFactory.CreateFallbackError() : _responseFactory.Create(error);
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
