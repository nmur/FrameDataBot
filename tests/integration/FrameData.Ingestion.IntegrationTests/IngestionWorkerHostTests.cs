using FrameData.Infrastructure.Dataset;
using FrameData.Ingestion.Catalog;
using FrameData.Ingestion.Hosting;
using FrameData.Ingestion.Publishing;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Source;
using Microsoft.Extensions.DependencyInjection;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class IngestionWorkerHostTests
{
    [Fact]
    public void AddFrameDataIngestionWorker_WiresCatalogPublisherSourceClientAndOrchestrator()
    {
        var root = Path.Combine(Path.GetTempPath(), $"framedata-worker-host-test-{Guid.NewGuid():N}");
        var services = new ServiceCollection();
        services.AddFrameDataIngestionWorker(new IngestionWorkerOptions
        {
            SourceBaseUrl = "http://example.test/source.php",
            DatasetRoot = root,
            ActiveDatasetPath = Path.Combine(root, "active")
        });

        using var provider = services.BuildServiceProvider();

        Assert.IsType<SupportedCharacterCatalog>(provider.GetRequiredService<ISupportedCharacterCatalog>());
        Assert.IsType<StaticDatasetPublisherOptions>(provider.GetRequiredService<StaticDatasetPublisherOptions>());
        Assert.IsType<StaticFrameDataDatasetLoader>(provider.GetRequiredService<StaticFrameDataDatasetLoader>());
        Assert.IsType<StaticDatasetPublisher>(provider.GetRequiredService<StaticDatasetPublisher>());
        Assert.IsType<SourceHttpClient>(provider.GetRequiredService<ISourceHttpClient>());
        Assert.IsType<IngestionOrchestrator>(provider.GetRequiredService<IngestionOrchestrator>());
    }

    [Fact]
    public void Program_DelegatesToOneShotWorkerInsteadOfConsoleTemplate()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(repositoryRoot, "src", "FrameData.Ingestion", "Program.cs"));

        Assert.Contains("IngestionWorkerProgram.RunAsync", program);
        Assert.DoesNotContain("Hello, World!", program);
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FrameDataBot.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
