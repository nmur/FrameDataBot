using FrameData.Bot.Hosting;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var options = BotRuntimeOptionsLoader.Load(builder.Configuration);

builder.Services.AddFrameDataBotServices(options);

var host = builder.Build();
await host.RunAsync();
