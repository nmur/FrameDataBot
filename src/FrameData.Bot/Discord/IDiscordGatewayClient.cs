using Discord;
using Discord.WebSocket;

namespace FrameData.Bot.Discord;

public interface IDiscordGatewayClient
{
    event Func<Task> Ready;
    event Func<SocketInteraction, Task> InteractionCreated;

    Task LoginAsync(string token);
    Task StartAsync();
    Task StopAsync();
    Task LogoutAsync();
}

public sealed class DiscordSocketGatewayClient : IDiscordGatewayClient
{
    private readonly DiscordSocketClient _client;

    public DiscordSocketGatewayClient(DiscordSocketClient client)
    {
        _client = client;
    }

    public event Func<Task> Ready
    {
        add => _client.Ready += value;
        remove => _client.Ready -= value;
    }

    public event Func<SocketInteraction, Task> InteractionCreated
    {
        add => _client.InteractionCreated += value;
        remove => _client.InteractionCreated -= value;
    }

    public Task LoginAsync(string token)
    {
        return _client.LoginAsync(TokenType.Bot, token);
    }

    public Task StartAsync()
    {
        return _client.StartAsync();
    }

    public Task StopAsync()
    {
        return _client.StopAsync();
    }

    public Task LogoutAsync()
    {
        return _client.LogoutAsync();
    }
}
