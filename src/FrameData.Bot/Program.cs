using FrameData.Bot.Hosting;
using FrameData.Shared.Logging;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
FrameDataLogging.Configure(builder.Logging, builder.Configuration, "FrameData.Bot");

var options = BotRuntimeOptionsLoader.Load(builder.Configuration);

builder.Services.AddFrameDataBotServices(options);

try
{
    var host = builder.Build();
    await host.RunAsync();
}
finally
{
    FrameDataLogging.CloseAndFlush();
}
