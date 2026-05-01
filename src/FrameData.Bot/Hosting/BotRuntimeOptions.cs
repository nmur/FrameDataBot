namespace FrameData.Bot.Hosting;

public sealed class BotRuntimeOptions
{
    public required string DiscordBotToken { get; init; }
    public required DiscordCommandRegistrationScope CommandRegistrationScope { get; init; }
    public required string BotGuildIds { get; init; }
    public required IReadOnlyList<ulong> DiscordGuildIds { get; init; }
    public required Uri BotApiBaseUrl { get; init; }
    public required string ActiveDatasetPath { get; init; }
}
