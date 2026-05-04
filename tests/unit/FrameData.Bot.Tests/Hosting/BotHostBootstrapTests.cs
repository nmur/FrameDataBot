using FrameData.Bot.Hosting;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace FrameData.Bot.Tests.Hosting;

public sealed class BotHostBootstrapTests
{
    [Fact]
    public void Load_WhenRequiredSettingsPresent_ReturnsGlobalCommandOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var options = BotRuntimeOptionsLoader.Load(configuration);

        options.DiscordBotToken.ShouldBe("token-value");
        options.CommandRegistrationScope.ShouldBe(DiscordCommandRegistrationScope.Global);
        options.BotGuildIds.ShouldBe(string.Empty);
        options.DiscordGuildIds.ShouldBeEmpty();
        options.BotApiBaseUrl.ShouldBe(new Uri("http://api:8080"));
        options.ActiveDatasetPath.ShouldBe("/data/framedata/active");
    }

    [Fact]
    public void Load_WhenGuildRegistrationScopePresent_ReturnsValidatedGuildOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["DISCORD_COMMAND_REGISTRATION_SCOPE"] = "guild",
                ["BOT_GUILD_IDS"] = "123456789,987654321",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var options = BotRuntimeOptionsLoader.Load(configuration);

        options.CommandRegistrationScope.ShouldBe(DiscordCommandRegistrationScope.Guild);
        options.BotGuildIds.ShouldBe("123456789,987654321");
        options.DiscordGuildIds.ShouldBe([123456789UL, 987654321UL]);
    }

    [Fact]
    public void Load_WhenLegacyGuildIdPresentInGuildScope_ReturnsValidatedOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["DISCORD_COMMAND_REGISTRATION_SCOPE"] = "guild",
                ["BOT_GUILD_ID"] = "123456789",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var options = BotRuntimeOptionsLoader.Load(configuration);

        options.BotGuildIds.ShouldBe("123456789");
        options.DiscordGuildIds.ShouldBe([123456789UL]);
    }

    [Fact]
    public void Load_WhenGuildIdsContainWhitespaceAndDuplicates_NormalisesThem()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["DISCORD_COMMAND_REGISTRATION_SCOPE"] = "guild",
                ["BOT_GUILD_IDS"] = " 123456789, 987654321, 123456789 ",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var options = BotRuntimeOptionsLoader.Load(configuration);

        options.BotGuildIds.ShouldBe("123456789,987654321");
        options.DiscordGuildIds.ShouldBe([123456789UL, 987654321UL]);
    }

    [Fact]
    public void Load_WhenDatasetRootPresent_DefaultsActiveDatasetPathUnderRoot()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["BOT_API_BASE_URL"] = "http://api:8080",
                ["FRAMEDATA_DATASET_ROOT"] = "/mounted/framedata"
            })
            .Build();

        var options = BotRuntimeOptionsLoader.Load(configuration);

        options.ActiveDatasetPath.ShouldBe("/mounted/framedata/active");
    }

    [Fact]
    public void Load_WhenTokenMissing_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
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
                ["BOT_API_BASE_URL"] = "/internal-api"
            })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => BotRuntimeOptionsLoader.Load(configuration));
        exception.Message.ShouldBe("BOT_API_BASE_URL must be an absolute URL.");
    }

    [Fact]
    public void Load_WhenGuildIdIsNotNumeric_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["DISCORD_COMMAND_REGISTRATION_SCOPE"] = "guild",
                ["BOT_GUILD_IDS"] = "123456789,not-a-snowflake",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => BotRuntimeOptionsLoader.Load(configuration));
        exception.Message.ShouldBe("BOT_GUILD_IDS must contain numeric Discord guild IDs separated by commas.");
    }

    [Fact]
    public void Load_WhenGuildScopeHasNoGuildIds_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["DISCORD_COMMAND_REGISTRATION_SCOPE"] = "guild",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => BotRuntimeOptionsLoader.Load(configuration));
        exception.Message.ShouldBe("BOT_GUILD_IDS or BOT_GUILD_ID is required when DISCORD_COMMAND_REGISTRATION_SCOPE=guild.");
    }

    [Fact]
    public void Load_WhenCommandRegistrationScopeIsUnknown_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DISCORD_BOT_TOKEN"] = "token-value",
                ["DISCORD_COMMAND_REGISTRATION_SCOPE"] = "private",
                ["BOT_API_BASE_URL"] = "http://api:8080"
            })
            .Build();

        var exception = Should.Throw<InvalidOperationException>(() => BotRuntimeOptionsLoader.Load(configuration));
        exception.Message.ShouldBe("DISCORD_COMMAND_REGISTRATION_SCOPE must be either global or guild.");
    }
}
