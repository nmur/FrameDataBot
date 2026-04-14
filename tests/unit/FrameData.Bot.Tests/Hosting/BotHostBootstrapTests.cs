using FrameData.Bot.Hosting;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace FrameData.Bot.Tests.Hosting;

public sealed class BotHostBootstrapTests
{
    [Fact]
    public void Load_WhenRequiredSettingsPresent_ReturnsValidatedOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["BOT_GUILD_ID"] = "123456789",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var options = BotRuntimeOptionsLoader.Load(configuration);

        options.DiscordBotToken.ShouldBe("token-value");
        options.BotGuildId.ShouldBe("123456789");
        options.BotApiBaseUrl.ShouldBe(new Uri("http://api:8080"));
    }

    [Fact]
    public void Load_WhenTokenMissing_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BOT_GUILD_ID"] = "123456789",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => BotRuntimeOptionsLoader.Load(configuration));
        exception.Message.ShouldBe("DISCORD_BOT_TOKEN is required.");
    }

    [Fact]
    public void Load_WhenApiBaseUrlIsRelative_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["BOT_GUILD_ID"] = "123456789",
                ["BOT_API_BASE_URL"] = "/internal-api"
            })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => BotRuntimeOptionsLoader.Load(configuration));
        exception.Message.ShouldBe("BOT_API_BASE_URL must be an absolute URL.");
    }
}
