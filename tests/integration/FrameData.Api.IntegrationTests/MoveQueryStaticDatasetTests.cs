using System.Net;
using System.Net.Http.Json;
using FrameData.Api.IntegrationTests.Fixtures;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FrameData.Api.IntegrationTests;

public sealed class MoveQueryStaticDatasetTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly string _datasetPath;
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _configuredFactory;

    public MoveQueryStaticDatasetTests(WebApplicationFactory<Program> factory)
    {
        _datasetPath = StaticDatasetFixtureWriter.CreateAsync(
            StaticDatasetFixtureWriter.Character(
                "chun-li",
                "Chun-Li",
                ["chun", "chun li"],
                StaticDatasetFixtureWriter.Move("chun-li", "2mk", startup: "5"),
                StaticDatasetFixtureWriter.Move("chun-li", "Kikouken (Jab)", displayOrder: 2, startup: "13"))).GetAwaiter().GetResult();

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
    public async Task GetMoveQuery_ReadsExactMoveFromStaticDataset()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=chun&moveInput=2mk");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Chun-Li", payload.Character);
        Assert.Equal("2mk", payload.MatchedMove);
        Assert.Equal("5", payload.FrameData.Startup);
    }

    [Fact]
    public async Task GetMoveQuery_ReadsAliasAndFuzzyMoveFromStaticDataset()
    {
        var response = await _client.GetAsync(
            "/v1/moves/query?character=chun-li&moveInput=light%20kikouken");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Kikouken (Jab)", payload.MatchedMove);
        Assert.Equal("Alias", payload.MatchedBy);
    }
}
