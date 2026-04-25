using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Ingestion.Backup;
using FrameData.Ingestion.IntegrationTests.Fixtures;
using Npgsql;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class BackupRestoreTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly DbConnectionFactory _connectionFactory;

    public BackupRestoreTests(PostgresContainerFixture postgres)
    {
        _connectionFactory = new DbConnectionFactory(postgres.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await new SchemaBootstrapper(_connectionFactory).RunAsync();
        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("TRUNCATE ingestion_run_character_statuses, ingestion_runs, moves, characters RESTART IDENTITY CASCADE;", connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExportAndImportAsync_RoundTripsManifestPerCharacterFilesAndDataset()
    {
        var backupDirectory = CreateTempDirectory();
        try
        {
            var datasetRepository = new FrameDataDatasetRepository(_connectionFactory);
            var backupService = new FrameDataBackupService(datasetRepository);
            await datasetRepository.ReplaceAsync(
            [
                new Character
                {
                    Id = "makoto",
                    Game = "sf3_3s",
                    Name = "Makoto",
                    SourceCharacterId = 17,
                    DisplayOrder = 17,
                    Aliases = ["mak"]
                }
            ],
            [
                CreateMove("makoto-normals-2mk", "makoto", "Makoto", "2mk", "6")
            ]);

            var exportedManifest = await backupService.ExportAsync(backupDirectory);
            await datasetRepository.ReplaceAsync(
            [
                new Character
                {
                    Id = "ken",
                    Game = "sf3_3s",
                    Name = "Ken",
                    SourceCharacterId = 11,
                    DisplayOrder = 11
                }
            ],
            [
                CreateMove("ken-normals-5hp", "ken", "Ken", "5hp", "4")
            ]);

            var restoredManifest = await backupService.ImportAsync(backupDirectory);
            var moveRepository = new MoveRepository(_connectionFactory);
            var restoredMove = await moveRepository.FindExactMoveAsync("mak", "2mk");
            var replacedMove = await moveRepository.FindExactMoveAsync("ken", "5hp");

            Assert.Equal(1, exportedManifest.CharacterCount);
            Assert.Equal(1, restoredManifest.CharacterCount);
            Assert.True(File.Exists(Path.Combine(backupDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(backupDirectory, "characters", "makoto.json")));
            Assert.NotNull(restoredMove);
            Assert.Equal("6", restoredMove.FrameData.Startup);
            Assert.Null(replacedMove);

            await using var manifestStream = File.OpenRead(Path.Combine(backupDirectory, "manifest.json"));
            var manifest = await JsonSerializer.DeserializeAsync<FrameDataBackupManifest>(
                manifestStream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(manifest);
            Assert.Equal("makoto", Assert.Single(manifest.Characters).Id);
        }
        finally
        {
            DeleteTempDirectory(backupDirectory);
        }
    }

    private static Move CreateMove(string id, string characterId, string characterName, string canonicalName, string startup)
        => new()
        {
            Id = id,
            CharacterId = characterId,
            Game = "sf3_3s",
            CharacterName = characterName,
            Section = "Normals",
            CanonicalName = canonicalName,
            DisplayOrder = 1,
            FrameData = new MoveFrameData
            {
                Startup = startup,
                Active = "3",
                Recovery = "17"
            }
        };

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"framedata-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteTempDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
