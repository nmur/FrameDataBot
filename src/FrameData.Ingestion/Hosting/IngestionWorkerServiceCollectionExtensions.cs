using FrameData.Domain.MoveLookup;
using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Infrastructure.Storage;
using FrameData.Ingestion.Catalog;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;
using Microsoft.Extensions.DependencyInjection;

namespace FrameData.Ingestion.Hosting;

public static class IngestionWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddFrameDataIngestionWorker(this IServiceCollection services, IngestionWorkerOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<ISupportedCharacterCatalog, SupportedCharacterCatalog>();
        services.AddSingleton(new DbConnectionFactory(options.PostgresConnectionString));
        services.AddSingleton<SchemaBootstrapper>();
        services.AddSingleton<CharacterRepository>();
        services.AddSingleton<MoveRepository>();
        services.AddSingleton<FrameDataDatasetRepository>();
        services.AddSingleton<IMoveQueryRepository>(sp => sp.GetRequiredService<MoveRepository>());
        services.AddSingleton<IngestionRunRepository>();
        services.AddSingleton<CharacterJsonExportService>();
        services.AddSingleton<CharacterSectionParser>();
        services.AddSingleton(sp => new CharacterExportWorkflow(
            sp.GetRequiredService<CharacterJsonExportService>(),
            options.ExportPath));
        services.AddHttpClient<ISourceHttpClient, SourceHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.SourceBaseUrl);
        });
        services.AddTransient<IngestionOrchestrator>();
        services.AddTransient<IngestionWorker>();
        return services;
    }
}
