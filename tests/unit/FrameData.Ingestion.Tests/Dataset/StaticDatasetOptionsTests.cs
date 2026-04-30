using FrameData.Ingestion.Hosting;
using FrameData.Ingestion.Publishing;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace FrameData.Ingestion.Tests.Dataset;

public sealed class StaticDatasetOptionsTests
{
    [Fact]
    public void Validate_WhenActivePathIsInsideDatasetRoot_ReturnsNoErrors()
    {
        var root = Path.Combine(Path.GetTempPath(), $"framedata-dataset-{Guid.NewGuid():N}");
        var options = new StaticDatasetPublisherOptions
        {
            DatasetRoot = root,
            ActiveDatasetPath = Path.Combine(root, "active")
        };

        options.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WhenActivePathEscapesDatasetRoot_ReturnsError()
    {
        var root = Path.Combine(Path.GetTempPath(), $"framedata-dataset-{Guid.NewGuid():N}");
        var options = new StaticDatasetPublisherOptions
        {
            DatasetRoot = root,
            ActiveDatasetPath = Path.Combine(Path.GetTempPath(), $"other-dataset-{Guid.NewGuid():N}", "active")
        };

        options.Validate().ShouldContain("FRAMEDATA_ACTIVE_DATASET_PATH must be inside FRAMEDATA_DATASET_ROOT.");
    }

    [Fact]
    public void FromConfiguration_ReadsStaticDatasetEnvironmentKeys()
    {
        var root = Path.Combine(Path.GetTempPath(), $"framedata-dataset-{Guid.NewGuid():N}");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["INGESTION_SOURCE_BASE_URL"] = "http://example.test/source.php",
                ["FRAMEDATA_DATASET_ROOT"] = root,
                ["FRAMEDATA_ACTIVE_DATASET_PATH"] = Path.Combine(root, "active"),
                ["characters"] = "makoto,chun-li"
            })
            .Build();

        var options = IngestionWorkerOptions.FromConfiguration(configuration);

        options.SourceBaseUrl.ShouldBe("http://example.test/source.php");
        options.DatasetRoot.ShouldBe(root);
        options.ActiveDatasetPath.ShouldBe(Path.Combine(root, "active"));
        options.CharacterIds.ShouldBe(["makoto", "chun-li"]);
        options.Validate().ShouldBeEmpty();
    }
}
