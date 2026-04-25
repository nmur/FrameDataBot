using System.Net;
using System.Net.Http.Json;
using FrameData.Api.IntegrationTests.Fixtures;
using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using FrameData.Infrastructure.Persistence;
using FrameData.Infrastructure.Persistence.Repositories;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FrameData.Api.IntegrationTests;

[Collection(ApiPostgresCollection.Name)]
public sealed class MoveQueryPostgresPersistenceTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly HttpClient _client;

    public MoveQueryPostgresPersistenceTests(WebApplicationFactory<Program> factory, PostgresContainerFixture postgres)
    {
        _connectionFactory = new DbConnectionFactory(postgres.ConnectionString);
        Environment.SetEnvironmentVariable("POSTGRES_CONNECTION_STRING", postgres.ConnectionString);
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["POSTGRES_CONNECTION_STRING"] = postgres.ConnectionString
                });
            });
        }).CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand("TRUNCATE ingestion_run_character_statuses, ingestion_runs, moves, characters RESTART IDENTITY CASCADE;", connection);
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMoveQuery_ReadsRowsInsertedThroughPostgresRepositories()
    {
        var characterRepository = new CharacterRepository(_connectionFactory);
        var moveRepository = new MoveRepository(_connectionFactory);
        await characterRepository.UpsertAsync(new Character
        {
            Id = "chun-li",
            Game = "sf3_3s",
            Name = "Chun-Li",
            SourceCharacterId = 16,
            DisplayOrder = 16,
            Aliases = ["chun", "chun li"]
        });
        await moveRepository.UpsertMovesAsync("chun-li",
        [
            new Move
            {
                Id = "chun-li-normals-2mk",
                CharacterId = "chun-li",
                Game = "sf3_3s",
                CharacterName = "Chun-Li",
                Section = "Normals",
                CanonicalName = "2mk",
                FrameData = new MoveFrameData
                {
                    Startup = "5",
                    Active = "3",
                    Recovery = "14",
                    OnHit = "+2",
                    OnBlock = "-1"
                }
            }
        ]);

        var response = await _client.GetAsync("/v1/moves/query?character=chun&moveInput=2mk");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Chun-Li", payload.Character);
        Assert.Equal("2mk", payload.MatchedMove);
        Assert.Equal("5", payload.FrameData.Startup);
    }
}
