using FrameData.Domain.Characters;
using FrameData.Domain.Ingestion;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Ingestion.IntegrationTests.Fixtures;
using Npgsql;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class PostgresRepositoryPersistenceTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private readonly DbConnectionFactory _connectionFactory;

    public PostgresRepositoryPersistenceTests(PostgresContainerFixture postgres)
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
    public async Task Repositories_InsertAndUpsertCharacterMoveAndIngestionRunRows()
    {
        var characterRepository = new CharacterRepository(_connectionFactory);
        var moveRepository = new MoveRepository(_connectionFactory);
        var runRepository = new IngestionRunRepository(_connectionFactory);

        var character = new Character
        {
            Id = "makoto",
            Game = "sf3_3s",
            Name = "Makoto",
            SourceCharacterId = 17,
            DisplayOrder = 17,
            Aliases = ["mak"]
        };
        await characterRepository.UpsertAsync(character);
        await moveRepository.UpsertMovesAsync(character.Id,
        [
            new Move
            {
                Id = "makoto-normals-2mk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "2mk",
                DisplayOrder = 1,
                FrameData = new MoveFrameData
                {
                    Startup = "6",
                    Active = "3",
                    Recovery = "17",
                    OnHit = "+1",
                    OnBlock = "-2",
                    FrameAdvantage = "-2"
                }
            }
        ]);

        var run = new IngestionRun
        {
            Id = "run-1",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Status = "Succeeded",
            CharactersProcessed = 1,
            MovesProcessed = 1,
            CharacterStatuses =
            {
                new IngestionRunCharacterStatus
                {
                    CharacterId = "makoto",
                    SourceCharacterId = 17,
                    Status = "Succeeded",
                    MovesProcessed = 1
                }
            }
        };
        await runRepository.SaveAsync(run);

        var persistedCharacter = await characterRepository.GetByIdAsync("makoto");
        var persistedMoves = await moveRepository.GetByCharacterIdAsync("makoto");
        var foundMove = await moveRepository.FindExactMoveAsync("mak", "2mk");
        var persistedRun = await runRepository.GetByIdAsync("run-1");

        Assert.NotNull(persistedCharacter);
        Assert.Equal(17, persistedCharacter.SourceCharacterId);
        Assert.Single(persistedMoves);
        Assert.NotNull(foundMove);
        Assert.NotNull(persistedRun);
        Assert.Single(persistedRun.CharacterStatuses);
    }

    [Fact]
    public async Task DatasetRepository_ReplaceAsync_ReplacesEntireCharacterAndMoveDataset()
    {
        var datasetRepository = new FrameDataDatasetRepository(_connectionFactory);
        var moveRepository = new MoveRepository(_connectionFactory);

        await datasetRepository.ReplaceAsync(
            [
                new Character
                {
                    Id = "makoto",
                    Game = "sf3_3s",
                    Name = "Makoto",
                    SourceCharacterId = 17,
                    DisplayOrder = 17
                }
            ],
            [
                CreateMove("makoto-normals-2mk", "makoto", "Makoto", "2mk")
            ]);

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
                CreateMove("ken-normals-5hp", "ken", "Ken", "5hp")
            ]);

        var staleMakotoMove = await moveRepository.FindExactMoveAsync("makoto", "2mk");
        var currentKenMove = await moveRepository.FindExactMoveAsync("ken", "5hp");

        Assert.Null(staleMakotoMove);
        Assert.NotNull(currentKenMove);
    }

    private static Move CreateMove(string id, string characterId, string characterName, string canonicalName)
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
                Startup = "6",
                Active = "3",
                Recovery = "17"
            }
        };
}
