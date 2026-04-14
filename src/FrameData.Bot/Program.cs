using FrameData.Bot.Api;
using FrameData.Bot.Commands;
using FrameData.Bot.Formatting;
using FrameData.Bot.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var options = BotRuntimeOptionsLoader.Load(builder.Configuration);

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<MoveCommandParser>();
builder.Services.AddSingleton<MoveResponseFormatter>();
builder.Services.AddSingleton<MoveCommandHandler>();
builder.Services.AddHttpClient<IMoveQueryApiClient, MoveQueryApiClient>(client =>
{
    client.BaseAddress = options.BotApiBaseUrl;
});
builder.Services.AddHostedService<BotRuntimeService>();

var host = builder.Build();
await host.RunAsync();
