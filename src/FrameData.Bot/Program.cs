using FrameData.Bot.Hosting;
using FrameData.Shared.Logging;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
using var logger = FrameDataLogging.CreateLogger(builder.Configuration, "FrameData.Bot");
FrameDataLogging.Configure(builder.Logging, logger);

var options = BotRuntimeOptionsLoader.Load(builder.Configuration);

builder.Services.AddFrameDataBotServices(options);

try
{
    var host = builder.Build();
    await host.RunAsync();
}
finally
{
    logger.Dispose();
}
