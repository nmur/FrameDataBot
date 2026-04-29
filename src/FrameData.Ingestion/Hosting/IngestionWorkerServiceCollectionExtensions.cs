using FrameData.Infrastructure.Dataset;
using FrameData.Ingestion.Catalog;
using FrameData.Ingestion.Media;
using FrameData.Ingestion.Publishing;
using FrameData.Ingestion.Services;
using FrameData.Domain.Media;
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
        services.AddSingleton(new StaticDatasetPublisherOptions
        {
            DatasetRoot = options.DatasetRoot,
            ActiveDatasetPath = options.ActiveDatasetPath
        });
        services.AddSingleton<StaticFrameDataDatasetLoader>();
        services.AddSingleton<StaticDatasetPublisher>();
        services.AddSingleton<CharacterSectionParser>();
        services.AddSingleton<HitboxDisplayParser>();
        services.AddSingleton<RepresentativeFrameSelector>();
        services.AddSingleton<HitboxCanvasRenderer>();
        services.AddSingleton<MoveImageDatasetStorageService>();
        services.AddHttpClient<SourceHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.SourceBaseUrl);
        });
        services.AddTransient<ISourceHttpClient>(sp => sp.GetRequiredService<SourceHttpClient>());
        services.AddTransient<IHitboxSourceClient>(sp => sp.GetRequiredService<SourceHttpClient>());
        services.AddTransient<IngestionOrchestrator>();
        services.AddTransient<IngestionWorker>();
        return services;
    }
}
