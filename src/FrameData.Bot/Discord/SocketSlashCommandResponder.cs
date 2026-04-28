using Discord;
using Discord.WebSocket;

namespace FrameData.Bot.Discord;

public sealed class SocketSlashCommandResponder : IDiscordInteractionResponder
{
    private readonly SocketSlashCommand _command;

    public SocketSlashCommandResponder(SocketSlashCommand command)
    {
        _command = command;
    }

    public Task DeferAsync(bool ephemeral = false)
    {
        return _command.DeferAsync(ephemeral);
    }

    public Task RespondAsync(string content, Embed? embed = null, bool ephemeral = false)
    {
        return _command.RespondAsync(content, embed: embed, ephemeral: ephemeral);
    }

    public Task FollowupAsync(string content, Embed? embed = null, bool ephemeral = false)
    {
        return _command.FollowupAsync(content, embed: embed, ephemeral: ephemeral);
    }
}
