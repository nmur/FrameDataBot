using FrameData.Api.Endpoints;
using FrameData.Domain.MoveLookup;
using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Infrastructure.Storage;
using FrameData.Ingestion.Catalog;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;
using FrameData.Shared.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
FrameDataLogging.Configure(builder.Logging, builder.Configuration, "FrameData.Api");
builder.Services.AddSerilog(Log.Logger, dispose: false);

var postgresConnectionString = builder.Configuration["POSTGRES_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("POSTGRES_CONNECTION_STRING is required.");

builder.Services.AddSingleton(new DbConnectionFactory(postgresConnectionString));
builder.Services.AddSingleton<SchemaBootstrapper>();
builder.Services.AddSingleton<ISupportedCharacterCatalog, SupportedCharacterCatalog>();
builder.Services.AddSingleton<CharacterRepository>();
builder.Services.AddSingleton<MoveRepository>();
builder.Services.AddSingleton<FrameDataDatasetRepository>();
builder.Services.AddSingleton<IMoveQueryRepository>(sp => sp.GetRequiredService<MoveRepository>());
builder.Services.AddSingleton<ExactMoveLookupService>();
builder.Services.AddSingleton<IngestionRunRepository>();
builder.Services.AddSingleton<CharacterJsonExportService>();
builder.Services.AddSingleton<CharacterSectionParser>();
builder.Services.AddSingleton(sp =>
{
    var exportPath = builder.Configuration["Ingestion:ExportPath"] ?? Path.Combine("exports", "characters");
    return new CharacterExportWorkflow(sp.GetRequiredService<CharacterJsonExportService>(), exportPath);
});
builder.Services.AddHttpClient<ISourceHttpClient, SourceHttpClient>((sp, client) =>
{
    var baseUrl = builder.Configuration["Ingestion:SourceBaseUrl"] ?? "http://ensabahnur.free.fr/BastonNew/index.php";
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddTransient<IngestionOrchestrator>();

try
{
    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    await app.Services.GetRequiredService<SchemaBootstrapper>().RunAsync();

    app.MapGet("/", () => "Hello World!");
    app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    app.MapMoveQueryEndpoint();
    app.MapIngestionEndpoints();

    await app.RunAsync();
}
finally
{
    FrameDataLogging.CloseAndFlush();
}

public partial class Program;
