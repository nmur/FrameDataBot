using Discord;
using Discord.WebSocket;
using FrameData.Bot.Formatting;

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

    public Task RespondAsync(
        string? content = null,
        Embed? embed = null,
        bool ephemeral = false,
        DiscordMoveAttachment? attachment = null,
        MessageComponent? components = null)
    {
        if (attachment is not null && File.Exists(attachment.FilePath))
        {
            return _command.RespondWithFileAsync(
                attachment.FilePath,
                attachment.FileName,
                text: content,
                embed: embed,
                ephemeral: ephemeral,
                components: components);
        }

        return _command.RespondAsync(content, embed: embed, ephemeral: ephemeral, components: components);
    }

    public Task FollowupAsync(
        string? content = null,
        Embed? embed = null,
        bool ephemeral = false,
        DiscordMoveAttachment? attachment = null,
        MessageComponent? components = null)
    {
        if (attachment is not null && File.Exists(attachment.FilePath))
        {
            return _command.FollowupWithFileAsync(
                attachment.FilePath,
                attachment.FileName,
                text: content,
                embed: embed,
                ephemeral: ephemeral,
                components: components);
        }

        return _command.FollowupAsync(content, embed: embed, ephemeral: ephemeral, components: components);
    }
}
