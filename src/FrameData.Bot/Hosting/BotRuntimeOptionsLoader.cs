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

        var commandRegistrationScope = ParseCommandRegistrationScope(
            configuration["DISCORD_COMMAND_REGISTRATION_SCOPE"] ?? configuration["Bot:CommandRegistrationScope"]);
        var guildIdsRaw = FirstNonBlank(
            configuration["BOT_GUILD_IDS"],
            configuration["Bot:GuildIds"],
            configuration["BOT_GUILD_ID"],
            configuration["Bot:GuildId"]);
        var discordGuildIds = commandRegistrationScope == DiscordCommandRegistrationScope.Guild
            ? ParseRequiredGuildIds(guildIdsRaw)
            : [];

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
            CommandRegistrationScope = commandRegistrationScope,
            BotGuildIds = string.Join(",", discordGuildIds),
            DiscordGuildIds = discordGuildIds,
            BotApiBaseUrl = apiBaseUrl,
            ActiveDatasetPath = activeDatasetPath
        };
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static DiscordCommandRegistrationScope ParseCommandRegistrationScope(string? rawScope)
    {
        if (string.IsNullOrWhiteSpace(rawScope))
        {
            return DiscordCommandRegistrationScope.Global;
        }

        return rawScope.Trim().ToLowerInvariant() switch
        {
            "global" => DiscordCommandRegistrationScope.Global,
            "guild" or "guilds" => DiscordCommandRegistrationScope.Guild,
            _ => throw new InvalidOperationException("DISCORD_COMMAND_REGISTRATION_SCOPE must be either global or guild.")
        };
    }

    private static IReadOnlyList<ulong> ParseRequiredGuildIds(string? guildIdsRaw)
    {
        if (string.IsNullOrWhiteSpace(guildIdsRaw))
        {
            throw new InvalidOperationException("BOT_GUILD_IDS or BOT_GUILD_ID is required when DISCORD_COMMAND_REGISTRATION_SCOPE=guild.");
        }

        var guildIds = new List<ulong>();
        var seen = new HashSet<ulong>();

        foreach (var rawGuildId in guildIdsRaw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!ulong.TryParse(rawGuildId, out var guildId))
            {
                throw new InvalidOperationException("BOT_GUILD_IDS must contain numeric Discord guild IDs separated by commas.");
            }

            if (seen.Add(guildId))
            {
                guildIds.Add(guildId);
            }
        }

        if (guildIds.Count == 0)
        {
            throw new InvalidOperationException("BOT_GUILD_IDS must contain at least one Discord guild ID.");
        }

        return guildIds;
    }
}
