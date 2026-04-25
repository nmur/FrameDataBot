namespace FrameData.Bot.Discord;

public interface IDiscordInteractionResponder
{
    Task DeferAsync(bool ephemeral = false);
    Task RespondAsync(string content, bool ephemeral = false);
    Task FollowupAsync(string content, bool ephemeral = false);
}
