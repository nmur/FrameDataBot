namespace FrameData.Api.IntegrationTests.BotService;

public sealed class BotApiConnectivityConfigTests
{
    [Fact]
    public void DockerCompose_DeclaresBotApiBaseUrlAndApiDependency()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var composeContent = File.ReadAllText(Path.Combine(repositoryRoot, "docker-compose.yml"));

        Assert.Contains("bot:", composeContent);
        Assert.Contains("BOT_API_BASE_URL: ${BOT_API_BASE_URL:-http://api:8080}", composeContent);
        Assert.Contains("depends_on:", composeContent);
        Assert.Contains("- api", composeContent);
    }

    [Fact]
    public void EnvExample_ProvidesBotApiBaseUrlDefault()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var envContent = File.ReadAllText(Path.Combine(repositoryRoot, ".env.example"));

        Assert.Contains("BOT_API_BASE_URL=http://api:8080", envContent);
    }

    [Fact]
    public void ProductionCompose_UsesRegistryImagesAndStableTagDefault()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var composeContent = File.ReadAllText(Path.Combine(repositoryRoot, "docker-compose.prod.yml"));

        Assert.Contains("image: ${FRAMEDATA_IMAGE_REPOSITORY:-ghcr.io/nmur}/framedata-api:${FRAMEDATA_IMAGE_TAG:-stable}", composeContent);
        Assert.Contains("image: ${FRAMEDATA_IMAGE_REPOSITORY:-ghcr.io/nmur}/framedata-bot:${FRAMEDATA_IMAGE_TAG:-stable}", composeContent);
        Assert.Contains("pull_policy: always", composeContent);
        Assert.DoesNotContain("\"5432:5432\"", composeContent);
        Assert.DoesNotContain("ingestion:", composeContent);
    }

    [Fact]
    public void ProductionEnvExample_DefaultsToStableRegistryDeployment()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var envContent = File.ReadAllText(Path.Combine(repositoryRoot, ".env.prod.example"));

        Assert.Contains("FRAMEDATA_IMAGE_REPOSITORY=ghcr.io/nmur", envContent);
        Assert.Contains("FRAMEDATA_IMAGE_TAG=stable", envContent);
        Assert.Contains("BOT_API_BASE_URL=http://api:8080", envContent);
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
