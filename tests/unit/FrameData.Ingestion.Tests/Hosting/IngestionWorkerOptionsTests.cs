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
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["POSTGRES_CONNECTION_STRING"] = "Host=localhost;Database=framedata",
                ["INGESTION_SOURCE_BASE_URL"] = "http://example.test/source.php",
                ["FRAMEDATA_EXPORTS_PATH"] = "/tmp/framedata",
                ["characters"] = "makoto,chun-li"
            })
            .Build();

        var options = IngestionWorkerOptions.FromConfiguration(configuration);

        options.PostgresConnectionString.ShouldBe("Host=localhost;Database=framedata");
        options.SourceBaseUrl.ShouldBe("http://example.test/source.php");
        options.ExportPath.ShouldBe("/tmp/framedata");
        options.CharacterIds.ShouldBe(["makoto", "chun-li"]);
        options.Validate().ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WhenRequiredConfigurationMissing_ReturnsErrors()
    {
        var options = new IngestionWorkerOptions
        {
            PostgresConnectionString = "",
            SourceBaseUrl = "",
            ExportPath = ""
        };

        var errors = options.Validate();

        errors.ShouldContain("POSTGRES_CONNECTION_STRING is required.");
        errors.ShouldContain("Ingestion source base URL is required.");
        errors.ShouldContain("Frame data export path is required.");
    }

    [Fact]
    public void Parse_WhenBackupCommandProvided_ReadsOutputPathAndPreservesConfigurationArgs()
    {
        var command = IngestionWorkerCommand.Parse(["backup", "--out", "/tmp/backup", "--characters=makoto"]);

        command.Mode.ShouldBe(IngestionWorkerMode.Backup);
        command.BackupPath.ShouldBe("/tmp/backup");
        command.ConfigurationArgs.ShouldBe(["--characters=makoto"]);
    }

    [Fact]
    public void Parse_WhenRestoreCommandProvided_ReadsInputPath()
    {
        var command = IngestionWorkerCommand.Parse(["restore", "--in=/tmp/backup"]);

        command.Mode.ShouldBe(IngestionWorkerMode.Restore);
        command.RestorePath.ShouldBe("/tmp/backup");
    }

    [Fact]
    public void Validate_WhenBackupPathMissing_ReturnsBackupErrorOnlyForBackupMode()
    {
        var options = new IngestionWorkerOptions
        {
            Mode = IngestionWorkerMode.Backup,
            PostgresConnectionString = "Host=localhost",
            BackupPath = ""
        };

        var errors = options.Validate();

        errors.ShouldContain("Backup output path is required.");
        errors.ShouldNotContain("Ingestion source base URL is required.");
        errors.ShouldNotContain("Frame data export path is required.");
    }

    [Fact]
    public void Validate_WhenRestorePathMissing_ReturnsRestoreErrorOnlyForRestoreMode()
    {
        var options = new IngestionWorkerOptions
        {
            Mode = IngestionWorkerMode.Restore,
            PostgresConnectionString = "Host=localhost",
            RestorePath = ""
        };

        var errors = options.Validate();

        errors.ShouldContain("Restore input path is required.");
        errors.ShouldNotContain("Ingestion source base URL is required.");
        errors.ShouldNotContain("Frame data export path is required.");
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
