using Microsoft.Extensions.Configuration;

namespace FrameData.Bot.Hosting;

public static class BotRuntimeOptionsLoader
{
    public static BotRuntimeOptions Load(IConfiguration configuration)
    {
        var token = configuration["DISCORD_BOT_TOKEN"] ?? configuration["Bot:DiscordBotToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("DISCORD_BOT_TOKEN is required.");
        }

        var guildId = configuration["BOT_GUILD_ID"] ?? configuration["Bot:GuildId"];
        if (string.IsNullOrWhiteSpace(guildId))
        {
            throw new InvalidOperationException("BOT_GUILD_ID is required.");
        }

        if (!ulong.TryParse(guildId, out var discordGuildId))
        {
            throw new InvalidOperationException("BOT_GUILD_ID must be a numeric Discord guild ID.");
        }

        var apiBaseUrlRaw = configuration["BOT_API_BASE_URL"] ?? configuration["Bot:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrlRaw))
        {
            throw new InvalidOperationException("BOT_API_BASE_URL is required.");
        }

        if (!Uri.TryCreate(apiBaseUrlRaw, UriKind.Absolute, out var apiBaseUrl) ||
            (apiBaseUrl.Scheme != Uri.UriSchemeHttp && apiBaseUrl.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("BOT_API_BASE_URL must be an absolute URL.");
        }

        var datasetRoot = configuration["FRAMEDATA_DATASET_ROOT"]
            ?? configuration["FrameData:DatasetRoot"]
            ?? "/data/framedata";
        var activeDatasetPath = configuration["FRAMEDATA_ACTIVE_DATASET_PATH"]
            ?? configuration["FrameData:ActiveDatasetPath"]
            ?? Path.Combine(datasetRoot, "active");

        return new BotRuntimeOptions
        {
            DiscordBotToken = token,
            BotGuildId = guildId,
            DiscordGuildId = discordGuildId,
            BotApiBaseUrl = apiBaseUrl,
            ActiveDatasetPath = activeDatasetPath
        };
    }
}
