using System.Net;
using System.Net.Http.Json;
using FrameData.Api.IntegrationTests.Fixtures;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FrameData.Api.IntegrationTests;

public sealed class MoveQueryExactTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _configuredFactory;
    private string _datasetPath = "";

    public MoveQueryExactTests(WebApplicationFactory<Program> factory)
    {
        _datasetPath = StaticDatasetFixtureWriter.CreateAsync(
            StaticDatasetFixtureWriter.Character(
                "makoto",
                "Makoto",
                ["mak"],
                StaticDatasetFixtureWriter.Move("makoto", "2mk")),
            StaticDatasetFixtureWriter.Character(
                "akuma",
                "Akuma",
                [],
                StaticDatasetFixtureWriter.Move("akuma", "5lp"))).GetAwaiter().GetResult();

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

    [Fact]
    public async Task GetMoveQuery_WhenExactMatch_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=makoto&moveInput=2mk");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Makoto", payload.Character);
        Assert.Equal("2mk", payload.MatchedMove);
    }

    [Fact]
    public async Task GetMoveQuery_WhenAkumaQueriedAsGouki_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=gouki&moveInput=5lp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Akuma", payload.Character);
        Assert.Equal("5lp", payload.MatchedMove);
    }

    [Fact]
    public async Task GetMoveQuery_WhenMoveNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=makoto&moveInput=5lk");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("move_not_found", payload.Code);
    }

}
