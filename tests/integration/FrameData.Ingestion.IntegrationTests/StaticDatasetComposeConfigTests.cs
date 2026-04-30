namespace FrameData.Ingestion.IntegrationTests;

public sealed class StaticDatasetComposeConfigTests
{
    [Fact]
    public void RuntimeCompose_DoesNotContainPostgresOrIngestionServices()
    {
        var root = ResolveRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.yml"));

        Assert.DoesNotContain("postgres:", compose);
        Assert.DoesNotContain("ingestion:", compose);
        Assert.Contains("FRAMEDATA_ACTIVE_DATASET_PATH", compose);
        Assert.Contains(":ro", compose);
        Assert.Contains("name: ${FRAMEDATA_DOCKER_NETWORK:-framedatabot}", compose);
    }

    [Fact]
    public void IngestionCompose_ContainsOneShotIngestionServiceWithWritableDatasetMount()
    {
        var root = ResolveRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.ingestion.yml"));

        Assert.Contains("ingestion:", compose);
        Assert.DoesNotContain("postgres:", compose);
        Assert.Contains("FRAMEDATA_DATASET_ROOT", compose);
        Assert.Contains("FRAMEDATA_ACTIVE_DATASET_PATH", compose);
        Assert.Contains(":rw", compose);
        Assert.Contains("external: true", compose);
        Assert.Contains("name: ${FRAMEDATA_DOCKER_NETWORK:-framedatabot}", compose);
    }

    [Fact]
    public void ProductionCompose_DoesNotContainPostgresOrIngestionServices()
    {
        var root = ResolveRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.prod.yml"));

        Assert.DoesNotContain("postgres:", compose);
        Assert.DoesNotContain("ingestion:", compose);
        Assert.Contains("FRAMEDATA_ACTIVE_DATASET_PATH", compose);
        Assert.Contains("name: ${FRAMEDATA_DOCKER_NETWORK:-framedatabot}", compose);
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
