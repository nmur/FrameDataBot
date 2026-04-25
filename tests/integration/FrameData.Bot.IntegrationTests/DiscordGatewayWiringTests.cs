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
            BotGuildId = "123456789",
            DiscordGuildId = 123456789,
            BotApiBaseUrl = new Uri("http://api:8080")
        };

        services.AddFrameDataBotServices(options);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IDiscordGatewayClient>().ShouldNotBeNull();
        provider.GetRequiredService<DiscordCommandRegistrar>().ShouldNotBeNull();
        provider.GetRequiredService<FramedataInteractionHandler>().ShouldNotBeNull();
        provider.GetServices<IHostedService>().Single().ShouldBeOfType<BotRuntimeService>();
    }
}
