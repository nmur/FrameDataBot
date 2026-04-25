using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Ingestion.Catalog;
using FrameData.Ingestion.Hosting;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Source;
using Microsoft.Extensions.DependencyInjection;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class IngestionWorkerHostTests
{
    [Fact]
    public void AddFrameDataIngestionWorker_WiresCatalogRepositoriesSchemaBootstrapSourceClientAndOrchestrator()
    {
        var services = new ServiceCollection();
        services.AddFrameDataIngestionWorker(new IngestionWorkerOptions
        {
            PostgresConnectionString = "Host=localhost;Database=framedata",
            SourceBaseUrl = "http://example.test/source.php",
            ExportPath = Path.Combine(Path.GetTempPath(), "framedata-worker-host-test")
        });

        using var provider = services.BuildServiceProvider();

        Assert.IsType<SupportedCharacterCatalog>(provider.GetRequiredService<ISupportedCharacterCatalog>());
        Assert.IsType<DbConnectionFactory>(provider.GetRequiredService<DbConnectionFactory>());
        Assert.IsType<SchemaBootstrapper>(provider.GetRequiredService<SchemaBootstrapper>());
        Assert.IsType<CharacterRepository>(provider.GetRequiredService<CharacterRepository>());
        Assert.IsType<MoveRepository>(provider.GetRequiredService<MoveRepository>());
        Assert.IsType<FrameDataDatasetRepository>(provider.GetRequiredService<FrameDataDatasetRepository>());
        Assert.IsType<IngestionRunRepository>(provider.GetRequiredService<IngestionRunRepository>());
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
