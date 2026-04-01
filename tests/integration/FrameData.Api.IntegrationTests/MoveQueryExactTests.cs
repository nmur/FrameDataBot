using System.Net;
using System.Net.Http.Json;
using FrameData.Shared.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FrameData.Api.IntegrationTests;

public sealed class MoveQueryExactTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MoveQueryExactTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMoveQuery_WhenExactMatch_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=makoto&moveInput=2mk");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("makoto", payload.Character);
        Assert.Equal("2mk", payload.MatchedMove);
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
