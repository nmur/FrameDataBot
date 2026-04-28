using Discord;

namespace FrameData.Bot.Formatting;

public sealed class DiscordMoveResponse
{
    public required string Content { get; init; }
    public Embed? Embed { get; init; }
    public string? AttachmentFileName { get; init; }
    public bool IsEphemeral { get; init; }
}
