using FrameData.Bot.Api;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FrameData.Bot.Hosting;

public sealed class BotRuntimeService : BackgroundService
{
    private readonly ILogger<BotRuntimeService> _logger;
    private readonly BotRuntimeOptions _options;
    private readonly IMoveQueryApiClient _apiClient;

    public BotRuntimeService(
        ILogger<BotRuntimeService> logger,
        BotRuntimeOptions options,
        IMoveQueryApiClient apiClient)
    {
        _logger = logger;
        _options = options;
        _apiClient = apiClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Bot runtime started with API base URL {ApiBaseUrl} for guild {GuildId}.",
            _options.BotApiBaseUrl,
            _options.BotGuildId);

        // This keeps the bot service process alive in containerized deployments.
        // Discord gateway command handling will be integrated in subsequent tasks.
        while (!stoppingToken.IsCancellationRequested)
        {
            _ = _apiClient;
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
