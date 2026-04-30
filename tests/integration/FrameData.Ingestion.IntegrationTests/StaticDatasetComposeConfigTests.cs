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
        Assert.DoesNotContain("datalust/seq", compose);
        Assert.DoesNotContain("with-seq", compose);
        Assert.Contains("FRAMEDATA_DATASET_ROOT", compose);
        Assert.Contains("FRAMEDATA_ACTIVE_DATASET_PATH", compose);
        Assert.Contains("SEQ_SERVER_URL", compose);
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

    [Fact]
    public void IngestionEnvExample_DeclaresSharedImageDatasetNetworkAndLoggingSettings()
    {
        var root = ResolveRepositoryRoot();
        var env = File.ReadAllText(Path.Combine(root, ".env.ingestion.example"));

        Assert.Contains("FRAMEDATA_IMAGE_REPOSITORY=ghcr.io/nmur", env);
        Assert.Contains("FRAMEDATA_IMAGE_TAG=stable", env);
        Assert.Contains("FRAMEDATA_DATASET_HOST_ROOT=./data/framedata", env);
        Assert.Contains("FRAMEDATA_DATASET_ROOT=/data/framedata", env);
        Assert.Contains("FRAMEDATA_ACTIVE_DATASET_PATH=/data/framedata/active", env);
        Assert.Contains("FRAMEDATA_DOCKER_NETWORK=framedatabot", env);
        Assert.Contains("INGESTION_MEDIA_PILOT_SCOPE=", env);
        Assert.Contains("SEQ_SERVER_URL=http://seq", env);
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
