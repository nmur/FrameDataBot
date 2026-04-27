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
    private readonly MoveResponseFormatter _formatter;
    private readonly ILogger<FramedataInteractionHandler> _logger;

    public FramedataInteractionHandler(
        SlashCommandInteractionMapper mapper,
        IMoveQueryApiClient apiClient,
        MoveResponseFormatter formatter,
        ILogger<FramedataInteractionHandler> logger)
    {
        _mapper = mapper;
        _apiClient = apiClient;
        _formatter = formatter;
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
            var content = FormatQueryResult(result.Response, result.Ambiguous, result.Error);
            _logger.LogInformation(
                "Discord /framedata interaction completed for character {Character} and move input {MoveInput} with result {ResultCode}.",
                invocation.Character,
                invocation.Move,
                result.Response is not null ? "ok" : result.Ambiguous is not null ? "ambiguous" : result.Error?.Code ?? "error");

            await responder.FollowupAsync(content);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle Discord /framedata interaction.");
            await responder.FollowupAsync("Unable to query frame data right now. Try again shortly.");
        }
    }

    private string FormatQueryResult(MoveQueryResponse? response, MoveAmbiguousResponse? ambiguous, ErrorResponse? error)
    {
        if (response is not null)
        {
            return _formatter.FormatSuccess(response);
        }

        if (ambiguous is not null)
        {
            return _formatter.FormatAmbiguous(ambiguous);
        }

        return _formatter.FormatError(error ?? new ErrorResponse
        {
            Code = "error",
            Message = "Unknown error"
        });
    }
}
