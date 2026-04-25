using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace FrameData.Bot.Discord;

public sealed class DiscordCommandRegistrar
{
    private readonly IDiscordCommandRegistrationClient _registrationClient;
    private readonly ILogger<DiscordCommandRegistrar> _logger;

    public DiscordCommandRegistrar(
        IDiscordCommandRegistrationClient registrationClient,
        ILogger<DiscordCommandRegistrar> logger)
    {
        _registrationClient = registrationClient;
        _logger = logger;
    }

    public async Task RegisterFramedataCommandAsync(ulong guildId)
    {
        await _registrationClient.RegisterGuildCommandAsync(guildId, FramedataSlashCommandDefinition.Build());
        _logger.LogInformation("Registered Discord slash command /{CommandName} for guild {GuildId}.", FramedataSlashCommandDefinition.CommandName, guildId);
    }
}

public interface IDiscordCommandRegistrationClient
{
    Task RegisterGuildCommandAsync(ulong guildId, ApplicationCommandProperties command);
}

public sealed class DiscordSocketCommandRegistrationClient : IDiscordCommandRegistrationClient
{
    private readonly DiscordSocketClient _client;

    public DiscordSocketCommandRegistrationClient(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task RegisterGuildCommandAsync(ulong guildId, ApplicationCommandProperties command)
    {
        var guild = _client.GetGuild(guildId);
        if (guild is null)
        {
            throw new InvalidOperationException($"Discord guild {guildId} is not available to this bot.");
        }

        return guild.BulkOverwriteApplicationCommandAsync([command]);
    }
}
