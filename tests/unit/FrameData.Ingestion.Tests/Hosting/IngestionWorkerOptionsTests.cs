using FrameData.Domain.Ingestion;
using FrameData.Ingestion.Hosting;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace FrameData.Ingestion.Tests.Hosting;

public sealed class IngestionWorkerOptionsTests
{
    [Fact]
    public void FromConfiguration_ReadsEnvironmentStyleKeysAndCharacters()
    {
        var root = Path.Combine(Path.GetTempPath(), $"framedata-dataset-{Guid.NewGuid():N}");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["INGESTION_SOURCE_BASE_URL"] = "http://example.test/source.php",
                ["FRAMEDATA_DATASET_ROOT"] = root,
                ["FRAMEDATA_ACTIVE_DATASET_PATH"] = Path.Combine(root, "active"),
                ["characters"] = "makoto,chun-li",
                ["INGESTION_MEDIA_PILOT_SCOPE"] = "ken/ken-normals-jab,ken/ken-specials-hadouken"
            })
            .Build();

        var options = IngestionWorkerOptions.FromConfiguration(configuration);

        options.SourceBaseUrl.ShouldBe("http://example.test/source.php");
        options.DatasetRoot.ShouldBe(root);
        options.ActiveDatasetPath.ShouldBe(Path.Combine(root, "active"));
        options.CharacterIds.ShouldBe(["makoto", "chun-li"]);
        options.RepresentativeFramePolicy.PilotMoveScope.ShouldBe(["ken/ken-normals-jab", "ken/ken-specials-hadouken"]);
        options.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WhenRequiredConfigurationMissing_ReturnsErrors()
    {
        var options = new IngestionWorkerOptions
        {
            SourceBaseUrl = "",
            DatasetRoot = "",
            ActiveDatasetPath = ""
        };

        var errors = options.Validate();

        errors.ShouldContain("Ingestion source base URL is required.");
        errors.ShouldContain("FRAMEDATA_DATASET_ROOT is required.");
        errors.ShouldContain("FRAMEDATA_ACTIVE_DATASET_PATH is required.");
    }

    [Fact]
    public void Parse_WhenIngestCommandProvided_PreservesConfigurationArgs()
    {
        var command = IngestionWorkerCommand.Parse(["ingest", "--characters=makoto"]);

        command.Mode.ShouldBe(IngestionWorkerMode.Ingest);
        command.ConfigurationArgs.ShouldBe(["--characters=makoto"]);
    }

    [Fact]
    public void Parse_WhenBackupCommandProvided_ReturnsUnknownCommandError()
    {
        var exception = Should.Throw<ArgumentException>(() => IngestionWorkerCommand.Parse(["backup", "--out", "/tmp/backup"]));
        exception.Message.ShouldBe("Unknown ingestion worker command: backup");
    }

    [Theory]
    [InlineData("Succeeded", IngestionWorkerExitCodes.Success)]
    [InlineData("PartiallySucceeded", IngestionWorkerExitCodes.PartialSuccess)]
    [InlineData("Failed", IngestionWorkerExitCodes.Failure)]
    public void MapRunStatus_MapsTerminalStatusToExitCode(string status, int expectedExitCode)
    {
        var run = new IngestionRun
        {
            Id = "run-1",
            StartedAt = DateTimeOffset.UtcNow,
            Status = status
        };

        IngestionWorkerExitCodeMapper.MapRunStatus(run).ShouldBe(expectedExitCode);
    }
}
