using Discord;
using FrameData.Bot.Formatting;

namespace FrameData.Bot.Discord;

public interface IDiscordInteractionResponder
{
    Task DeferAsync(bool ephemeral = false);
    Task RespondAsync(string? content = null, Embed? embed = null, bool ephemeral = false, DiscordMoveAttachment? attachment = null);
    Task FollowupAsync(string? content = null, Embed? embed = null, bool ephemeral = false, DiscordMoveAttachment? attachment = null);
}
