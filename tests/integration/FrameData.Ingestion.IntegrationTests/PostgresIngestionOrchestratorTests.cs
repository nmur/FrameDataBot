using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Infrastructure.Storage;
using FrameData.Ingestion.IntegrationTests.Fixtures;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;
using Npgsql;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class PostgresIngestionOrchestratorTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private const string SampleHtml = """
        <html><body>
          <h2>Normals</h2>
          <table>
            <tr><th>Move</th><th>Startup</th><th>Active</th><th>Recovery</th><th>On Hit</th><th>On Block</th><th>Frame Advantage</th></tr>
            <tr><td>2mk</td><td>6</td><td>3</td><td>17</td><td>+1</td><td>-2</td><td>-2</td></tr>
          </table>
        </body></html>
        """;

    private readonly PostgresContainerFixture _postgres;
    private readonly DbConnectionFactory _connectionFactory;

    public PostgresIngestionOrchestratorTests(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
        _connectionFactory = new DbConnectionFactory(_postgres.ConnectionString);
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
    public async Task RunAsync_WhenAllCharactersSucceed_WritesPostgresRowsJsonExportsAndCharacterStatuses()
    {
        var exportDirectory = CreateTempDirectory();
        try
        {
            var orchestrator = CreateOrchestrator(new FakeSourceHttpClient(_ => SampleHtml), exportDirectory);

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "makoto", CharacterName = "Makoto", SourceCharacterId = 17 },
                new IngestionCharacterScope { CharacterId = "ken", CharacterName = "Ken", SourceCharacterId = 11 }
            ]);

            Assert.Equal("Succeeded", run.Status);
            Assert.Equal(2, run.CharactersProcessed);
            Assert.Equal(2, run.CharacterStatuses.Count);
            Assert.True(File.Exists(Path.Combine(exportDirectory, "makoto.json")));
            Assert.True(File.Exists(Path.Combine(exportDirectory, "ken.json")));

            await using var connection = _connectionFactory.CreateOpenConnection();
            await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM moves;", connection);
            var moveCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
            Assert.Equal(2, moveCount);
        }
        finally
        {
            DeleteTempDirectory(exportDirectory);
        }
    }

    [Fact]
    public async Task RunAsync_WhenSomeCharactersFail_ReplacesDatasetWithSuccessfulScopesAndRecordsRetryStatus()
    {
        var exportDirectory = CreateTempDirectory();
        try
        {
            var orchestrator = CreateOrchestrator(new FakeSourceHttpClient(id =>
            {
                if (id == 16)
                {
                    throw new InvalidOperationException("source unavailable");
                }

                return SampleHtml;
            }), exportDirectory);

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "makoto", CharacterName = "Makoto", SourceCharacterId = 17 },
                new IngestionCharacterScope { CharacterId = "chun-li", CharacterName = "Chun-Li", SourceCharacterId = 16 }
            ]);

            Assert.Equal("PartiallySucceeded", run.Status);
            Assert.Equal(1, run.CharactersProcessed);
            Assert.Single(run.Errors);
            Assert.Contains(run.CharacterStatuses, status => status.CharacterId == "makoto" && status.Status == "Succeeded");
            Assert.Contains(run.CharacterStatuses, status => status.CharacterId == "chun-li" && status.Status == "Failed");
            Assert.True(File.Exists(Path.Combine(exportDirectory, "makoto.json")));
            Assert.False(File.Exists(Path.Combine(exportDirectory, "chun-li.json")));

            await using var connection = _connectionFactory.CreateOpenConnection();
            await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM moves;", connection);
            var moveCount = (long)(await command.ExecuteScalarAsync() ?? 0L);
            Assert.Equal(1, moveCount);
        }
        finally
        {
            DeleteTempDirectory(exportDirectory);
        }
    }

    [Fact]
    public async Task RunAsync_WhenEveryCharacterFails_LeavesPreviousDatasetIntact()
    {
        var exportDirectory = CreateTempDirectory();
        try
        {
            var datasetRepository = new FrameDataDatasetRepository(_connectionFactory);
            await datasetRepository.ReplaceAsync(
            [
                new()
                {
                    Id = "makoto",
                    Game = "sf3_3s",
                    Name = "Makoto",
                    SourceCharacterId = 17,
                    DisplayOrder = 17
                }
            ],
            [
                new()
                {
                    Id = "makoto-normals-2mk",
                    CharacterId = "makoto",
                    Game = "sf3_3s",
                    CharacterName = "Makoto",
                    Section = "Normals",
                    CanonicalName = "2mk",
                    DisplayOrder = 1,
                    FrameData = new() { Startup = "6", Active = "3", Recovery = "17" }
                }
            ]);

            var orchestrator = CreateOrchestrator(new FakeSourceHttpClient(_ =>
            {
                throw new InvalidOperationException("source unavailable");
            }), exportDirectory);

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "chun-li", CharacterName = "Chun-Li", SourceCharacterId = 16 }
            ]);

            var moveRepository = new MoveRepository(_connectionFactory);
            var existingMove = await moveRepository.FindExactMoveAsync("makoto", "2mk");

            Assert.Equal("Failed", run.Status);
            Assert.NotNull(existingMove);
        }
        finally
        {
            DeleteTempDirectory(exportDirectory);
        }
    }

    private IngestionOrchestrator CreateOrchestrator(ISourceHttpClient sourceClient, string exportDirectory)
        => new(
            sourceClient,
            new CharacterSectionParser(),
            new FrameDataDatasetRepository(_connectionFactory),
            new IngestionRunRepository(_connectionFactory),
            new CharacterExportWorkflow(new CharacterJsonExportService(), exportDirectory));

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"framedata-ingest-{Guid.NewGuid():N}");
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

    private sealed class FakeSourceHttpClient : ISourceHttpClient
    {
        private readonly Func<int, string> _resolver;

        public FakeSourceHttpClient(Func<int, string> resolver)
        {
            _resolver = resolver;
        }

        public Task<string> GetCharacterPageAsync(int sourceCharacterId, CancellationToken cancellationToken = default)
            => Task.FromResult(_resolver(sourceCharacterId));
    }
}
