using Discord;

namespace FrameData.Bot.Formatting;

public sealed class DiscordMoveResponse
{
    public string? Content { get; init; }
    public Embed? Embed { get; init; }
    public MessageComponent? Components { get; init; }
    public DiscordMoveAttachment? Attachment { get; init; }
    public bool IsEphemeral { get; init; }
}

public sealed record DiscordMoveAttachment(string FilePath, string FileName);
