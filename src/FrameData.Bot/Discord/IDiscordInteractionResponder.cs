using Discord;

namespace FrameData.Bot.Discord;

public interface IDiscordInteractionResponder
{
    Task DeferAsync(bool ephemeral = false);
    Task RespondAsync(string? content = null, Embed? embed = null, bool ephemeral = false);
    Task FollowupAsync(string? content = null, Embed? embed = null, bool ephemeral = false);
}
