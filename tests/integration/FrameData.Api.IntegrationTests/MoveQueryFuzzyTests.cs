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
public sealed class MoveQueryFuzzyTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly HttpClient _client;

    public MoveQueryFuzzyTests(WebApplicationFactory<Program> factory, PostgresContainerFixture postgres)
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

        var characterRepository = new CharacterRepository(_connectionFactory);
        var moveRepository = new MoveRepository(_connectionFactory);
        await characterRepository.UpsertAsync(new Character
        {
            Id = "makoto",
            Game = "sf3_3s",
            Name = "Makoto",
            SourceCharacterId = 17,
            DisplayOrder = 17,
            Aliases = ["mak"]
        });

        await moveRepository.UpsertMovesAsync("makoto",
        [
            new Move
            {
                Id = "makoto-normals-2hk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "2hk",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "8", Active = "4", Recovery = "20", OnHit = "KD", OnBlock = "-10" }
            },
            new Move
            {
                Id = "makoto-normals-5hk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "5hk",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "10", Active = "3", Recovery = "19", OnHit = "+1", OnBlock = "-3" }
            }
        ]);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("cr.HK")]
    [InlineData("sweep")]
    public async Task GetMoveQuery_WhenAliasMatchesSingleMove_ReturnsOk(string moveInput)
    {
        var response = await _client.GetAsync($"/v1/moves/query?character=makoto&moveInput={Uri.EscapeDataString(moveInput)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("2hk", payload.MatchedMove);
        Assert.Equal("Alias", payload.MatchedBy);
    }

    [Fact]
    public async Task GetMoveQuery_WhenInputIsAmbiguous_ReturnsMultipleChoicesWithCandidates()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=makoto&moveInput=hk");

        Assert.Equal(HttpStatusCode.MultipleChoices, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveAmbiguousResponse>();
        Assert.NotNull(payload);
        Assert.Contains(payload.Candidates, candidate => candidate.MoveName == "2hk");
        Assert.Contains(payload.Candidates, candidate => candidate.MoveName == "5hk");
    }

    [Fact]
    public async Task GetMoveQuery_WhenNoFuzzyCandidateMeetsThreshold_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=makoto&moveInput=zzzz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("move_not_found", payload.Code);
    }
}
