using Discord;
using Discord.WebSocket;
using FrameData.Bot.Api;
using FrameData.Bot.Discord;
using FrameData.Bot.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace FrameData.Bot.Hosting;

public static class BotServiceCollectionExtensions
{
    public static IServiceCollection AddFrameDataBotServices(this IServiceCollection services, BotRuntimeOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<MoveEmbedResponseFactory>();
        services.AddSingleton<SlashCommandInteractionMapper>();
        services.AddSingleton<FramedataInteractionHandler>();
        services.AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
        }));
        services.AddSingleton<IDiscordGatewayClient, DiscordSocketGatewayClient>();
        services.AddSingleton<IDiscordCommandRegistrationClient, DiscordSocketCommandRegistrationClient>();
        services.AddSingleton<DiscordCommandRegistrar>();
        services.AddHttpClient<IMoveQueryApiClient, MoveQueryApiClient>(client =>
        {
            client.BaseAddress = options.BotApiBaseUrl;
        });
        services.AddHostedService<BotRuntimeService>();

        return services;
    }
}
