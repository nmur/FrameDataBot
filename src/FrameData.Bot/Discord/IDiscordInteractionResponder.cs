using Discord;

namespace FrameData.Bot.Discord;

public interface IDiscordInteractionResponder
{
    Task DeferAsync(bool ephemeral = false);
    Task RespondAsync(string content, Embed? embed = null, bool ephemeral = false);
    Task FollowupAsync(string content, Embed? embed = null, bool ephemeral = false);
}
