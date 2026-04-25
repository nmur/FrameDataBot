using Discord.WebSocket;
using FrameData.Bot.Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FrameData.Bot.Hosting;

public sealed class BotRuntimeService : BackgroundService
{
    private readonly ILogger<BotRuntimeService> _logger;
    private readonly BotRuntimeOptions _options;
    private readonly IDiscordGatewayClient _gatewayClient;
    private readonly DiscordCommandRegistrar _commandRegistrar;
    private readonly FramedataInteractionHandler _interactionHandler;

    public BotRuntimeService(
        ILogger<BotRuntimeService> logger,
        BotRuntimeOptions options,
        IDiscordGatewayClient gatewayClient,
        DiscordCommandRegistrar commandRegistrar,
        FramedataInteractionHandler interactionHandler)
    {
        _logger = logger;
        _options = options;
        _gatewayClient = gatewayClient;
        _commandRegistrar = commandRegistrar;
        _interactionHandler = interactionHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Bot runtime started with API base URL {ApiBaseUrl} for guild {GuildId}.",
            _options.BotApiBaseUrl,
            _options.BotGuildId);

        _gatewayClient.Ready += RegisterCommandsAsync;
        _gatewayClient.InteractionCreated += HandleInteractionAsync;

        try
        {
            await _gatewayClient.LoginAsync(_options.DiscordBotToken);
            await _gatewayClient.StartAsync();
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Bot runtime stopping.");
        }
        finally
        {
            _gatewayClient.Ready -= RegisterCommandsAsync;
            _gatewayClient.InteractionCreated -= HandleInteractionAsync;
            await _gatewayClient.StopAsync();
            await _gatewayClient.LogoutAsync();
        }
    }

    private Task RegisterCommandsAsync()
    {
        return _commandRegistrar.RegisterFramedataCommandAsync(_options.DiscordGuildId);
    }

    private Task HandleInteractionAsync(SocketInteraction interaction)
    {
        if (interaction is SocketSlashCommand slashCommand)
        {
            return _interactionHandler.HandleSocketSlashCommandAsync(slashCommand);
        }

        return Task.CompletedTask;
    }
}
