using FrameData.Api.Endpoints;
using FrameData.Api.Responses;
using FrameData.Domain.Datasets;
using FrameData.Domain.MoveLookup;
using FrameData.Infrastructure.Dataset;
using FrameData.Shared.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
using var logger = FrameDataLogging.CreateLogger(builder.Configuration, "FrameData.Api");
FrameDataLogging.Configure(builder.Logging, logger);
builder.Services.AddSerilog(logger, dispose: false);

builder.Services.AddSingleton<StaticFrameDataDatasetLoader>();
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var activeDatasetPath = configuration["FRAMEDATA_ACTIVE_DATASET_PATH"]
        ?? configuration["FrameData:ActiveDatasetPath"]
        ?? Path.Combine("data", "framedata", "active");

    return sp.GetRequiredService<StaticFrameDataDatasetLoader>()
        .LoadAsync(activeDatasetPath)
        .GetAwaiter()
        .GetResult();
});
builder.Services.AddSingleton<IMoveQueryRepository, StaticMoveQueryRepository>();
builder.Services.AddSingleton<AliasNormalizer>();
builder.Services.AddSingleton<FuzzyMoveMatcher>();
builder.Services.AddSingleton<ExactMoveLookupService>();
builder.Services.AddSingleton<MoveDisambiguationResponseFactory>();

try
{
    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.MapGet("/", () => "Hello World!");
    app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    _ = app.Services.GetRequiredService<StaticFrameDataDataset>();
    app.MapMoveQueryEndpoint();

    await app.RunAsync();
}
finally
{
    logger.Dispose();
}

public partial class Program;
