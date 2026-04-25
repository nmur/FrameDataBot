using FrameData.Domain.Ingestion;
using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Infrastructure.Storage;
using FrameData.Ingestion.IntegrationTests.Fixtures;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;
using Npgsql;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class IngestionPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly DbConnectionFactory _connectionFactory;

    public IngestionPersistenceTests(PostgresContainerFixture postgres)
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

    private const string SampleHtml = """
        <html><body>
          <h2>Normals</h2>
          <table>
            <tr><th>Move</th><th>Startup</th><th>Active</th><th>Recovery</th><th>On Hit</th><th>On Block</th><th>Frame Advantage</th></tr>
            <tr><td>2mk</td><td>6</td><td>3</td><td>17</td><td>+1</td><td>-2</td><td>-2</td></tr>
          </table>
        </body></html>
        """;

    [Fact]
    public async Task RunAsync_WhenAllCharactersSucceed_PersistsDataAndMarksSucceeded()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var source = new FakeSourceHttpClient(_ => SampleHtml);
            var runRepository = new IngestionRunRepository(_connectionFactory);
            var moveRepository = new MoveRepository(_connectionFactory);
            var datasetRepository = new FrameDataDatasetRepository(_connectionFactory);
            var workflow = new CharacterExportWorkflow(new CharacterJsonExportService(), tempDirectory);
            var orchestrator = new IngestionOrchestrator(
                source,
                new CharacterSectionParser(),
                datasetRepository,
                runRepository,
                workflow);

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "makoto", CharacterName = "makoto", SourceCharacterId = 1 },
                new IngestionCharacterScope { CharacterId = "ken", CharacterName = "ken", SourceCharacterId = 2 }
            ]);

            Assert.Equal("Succeeded", run.Status);
            Assert.Equal(2, run.CharactersProcessed);
            Assert.True(run.MovesProcessed >= 2);

            var makotoMoves = await moveRepository.GetByCharacterIdAsync("makoto");
            Assert.NotEmpty(makotoMoves);
            Assert.True(File.Exists(Path.Combine(tempDirectory, "makoto.json")));
            Assert.True(File.Exists(Path.Combine(tempDirectory, "ken.json")));
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

    [Fact]
    public async Task RunAsync_WhenSomeCharactersFail_PersistsSuccessesAndMarksPartial()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            var source = new FakeSourceHttpClient(id =>
            {
                if (id == 2)
                {
                    throw new InvalidOperationException("Source unavailable");
                }

                return SampleHtml;
            });
            var runRepository = new IngestionRunRepository(_connectionFactory);
            var moveRepository = new MoveRepository(_connectionFactory);
            var datasetRepository = new FrameDataDatasetRepository(_connectionFactory);
            var workflow = new CharacterExportWorkflow(new CharacterJsonExportService(), tempDirectory);
            var orchestrator = new IngestionOrchestrator(
                source,
                new CharacterSectionParser(),
                datasetRepository,
                runRepository,
                workflow);

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "makoto", CharacterName = "makoto", SourceCharacterId = 1 },
                new IngestionCharacterScope { CharacterId = "ken", CharacterName = "ken", SourceCharacterId = 2 }
            ]);

            Assert.Equal("PartiallySucceeded", run.Status);
            Assert.Equal(1, run.CharactersProcessed);
            Assert.NotEmpty(run.Errors);

            var makotoMoves = await moveRepository.GetByCharacterIdAsync("makoto");
            Assert.NotEmpty(makotoMoves);
            Assert.True(File.Exists(Path.Combine(tempDirectory, "makoto.json")));
            Assert.False(File.Exists(Path.Combine(tempDirectory, "ken.json")));
        }
        finally
        {
            DeleteTempDirectory(tempDirectory);
        }
    }

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
