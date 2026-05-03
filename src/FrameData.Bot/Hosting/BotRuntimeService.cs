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
            "Bot runtime started with API base URL {ApiBaseUrl}. CommandRegistrationScope={CommandRegistrationScope}. GuildIds={GuildIds}. ActiveDatasetPath={ActiveDatasetPath}.",
            _options.BotApiBaseUrl,
            _options.CommandRegistrationScope,
            _options.BotGuildIds,
            _options.ActiveDatasetPath);

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

    private async Task RegisterCommandsAsync()
    {
        if (_options.CommandRegistrationScope == DiscordCommandRegistrationScope.Global)
        {
            await _commandRegistrar.RegisterGlobalFramedataCommandAsync();
            return;
        }

        foreach (var guildId in _options.DiscordGuildIds)
        {
            try
            {
                await _commandRegistrar.RegisterFramedataCommandAsync(guildId);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to register Discord slash commands for guild {GuildId}. Ensure the bot is installed in that guild with the bot and applications.commands scopes.",
                    guildId);
            }
        }
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
