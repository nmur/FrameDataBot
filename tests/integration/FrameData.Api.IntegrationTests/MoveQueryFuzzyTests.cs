using System.Net;
using System.Net.Http.Json;
using FrameData.Api.IntegrationTests.Fixtures;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FrameData.Api.IntegrationTests;

public sealed class MoveQueryFuzzyTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _configuredFactory;
    private readonly string _datasetPath;

    public MoveQueryFuzzyTests(WebApplicationFactory<Program> factory)
    {
        _datasetPath = StaticDatasetFixtureWriter.CreateAsync(
            StaticDatasetFixtureWriter.Character(
                "makoto",
                "Makoto",
                ["mak"],
                StaticDatasetFixtureWriter.Move("makoto", "2hk", displayOrder: 1, startup: "8"),
                StaticDatasetFixtureWriter.Move("makoto", "5hk", displayOrder: 2, startup: "10")),
            StaticDatasetFixtureWriter.Character(
                "alex",
                "Alex",
                [],
                StaticDatasetFixtureWriter.Move("alex", "LK", displayOrder: 1, startup: "4"),
                StaticDatasetFixtureWriter.Move("alex", "Spiral D.D.T. (LK)", displayOrder: 2, section: "Specials", startup: "7"),
                StaticDatasetFixtureWriter.Move("alex", "Spiral D.D.T. (MK)", displayOrder: 3, section: "Specials", startup: "7"),
                StaticDatasetFixtureWriter.Move("alex", "Spiral D.D.T. (HK)", displayOrder: 4, section: "Specials", startup: "7"))).GetAwaiter().GetResult();

        _configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FRAMEDATA_ACTIVE_DATASET_PATH"] = _datasetPath
                });
            });
        });
        _client = _configuredFactory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        _configuredFactory.Dispose();
        StaticDatasetFixtureWriter.Delete(_datasetPath);
        return Task.CompletedTask;
    }

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
    public async Task GetMoveQuery_WhenAlexDdtUsesStrengthAndShortName_ReturnsSpiralDdtSpecial()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=alex&moveInput=lk%20ddt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Spiral D.D.T. (LK)", payload.MatchedMove);
        Assert.Equal("Specials", payload.Section);
        Assert.Equal("Alias", payload.MatchedBy);
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
