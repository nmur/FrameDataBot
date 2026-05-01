using FrameData.Bot.Discord;
using FrameData.Bot.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace FrameData.Bot.IntegrationTests;

public sealed class DiscordGatewayWiringTests
{
    [Fact]
    public void AddFrameDataBotServices_RegistersGatewayInteractionAndHostedService()
    {
        var services = new ServiceCollection();
        var options = new BotRuntimeOptions
        {
            DiscordBotToken = "token-value",
            CommandRegistrationScope = DiscordCommandRegistrationScope.Global,
            BotGuildIds = "123456789,987654321",
            DiscordGuildIds = [123456789UL, 987654321UL],
            BotApiBaseUrl = new Uri("http://api:8080"),
            ActiveDatasetPath = "/data/framedata/active"
        };

        services.AddFrameDataBotServices(options);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDiscordGatewayClient>().ShouldNotBeNull();
        provider.GetRequiredService<DiscordCommandRegistrar>().ShouldNotBeNull();
        provider.GetRequiredService<FramedataInteractionHandler>().ShouldNotBeNull();
        provider.GetServices<IHostedService>().Single().ShouldBeOfType<BotRuntimeService>();
    }
}
