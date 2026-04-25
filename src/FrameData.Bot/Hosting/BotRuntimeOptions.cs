namespace FrameData.Bot.Hosting;

public sealed class BotRuntimeOptions
{
    public required string DiscordBotToken { get; init; }
    public required string BotGuildId { get; init; }
    public required ulong DiscordGuildId { get; init; }
    public required Uri BotApiBaseUrl { get; init; }
}
