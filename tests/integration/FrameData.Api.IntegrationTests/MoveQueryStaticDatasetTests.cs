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
                StaticDatasetFixtureWriter.Move(
                    "chun-li",
                    "2mk",
                    startup: "5",
                    onCrouchingHit: "+3",
                    sourceHitboxPath: "hitboxesDisplay.php?sMode=f&iChar=1&sMoveType=fd_normals&sAction=w&iMove=20",
                    media:
                    [
                        new StaticDatasetMoveMedia
                        {
                            Type = "RepresentativeActiveFrame",
                            Path = "media/chun-li/chun-li-normals-2mk/representative-active-frame.png",
                            SourceUrl = "http://example.test/hitboxesDisplay.php?iMove=20",
                            SourceFrameImageUrl = "http://example.test/frames/006.png",
                            SelectedFrame = "006",
                            SelectionStrategy = "largest-active-hitbox-area",
                            ActiveHitboxArea = 100,
                            OverlayHitboxes = ["P1_P", "P1_V", "P1_A", "P1_T", "P1_TA"],
                            CapturedAt = DateTimeOffset.UtcNow,
                            CaptureStatus = "Success"
                        }
                    ]),
                StaticDatasetFixtureWriter.Move(
                    "chun-li",
                    "Kikouken (Jab)",
                    displayOrder: 2,
                    startup: "13",
                    motion: "236P",
                    damage: "60",
                    stun: "7")),
            StaticDatasetFixtureWriter.Character(
                "makoto",
                "Makoto",
                [],
                StaticDatasetFixtureWriter.Move("makoto", "Hayate (LP)", displayOrder: 1, startup: "12"),
                StaticDatasetFixtureWriter.Move("makoto", "Hayate (MP)", displayOrder: 2, startup: "15"),
                StaticDatasetFixtureWriter.Move("makoto", "Hayate (HP)", displayOrder: 3, startup: "19"))).GetAwaiter().GetResult();

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
        Assert.Equal("+3", payload.FrameData.OnCrouchingHit);
        Assert.Equal("http://baston.esn3s.com/index.php?id=1", payload.CharacterFrameDataUrl);
        Assert.Equal("http://baston.esn3s.com/hitboxesDisplay_spritesheet.php?iChar=1&sMoveType=fd_normals&iMove=20", payload.MoveHitboxDisplayUrl);
        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/chunli/chunli_mk.html", payload.GameRestaurantMoveUrl);
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
        Assert.Equal("236P", payload.Motion);
        Assert.Equal("60", payload.Damage);
        Assert.Equal("7", payload.Stun);
        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/chunli/chunli_h2.html", payload.GameRestaurantMoveUrl);
    }

    [Fact]
    public async Task GetMoveQuery_WhenInputUsesMotionAlias_ReturnsMoveFromStaticDataset()
    {
        var response = await _client.GetAsync(
            "/v1/moves/query?character=chun-li&moveInput=qcf%20lp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Kikouken (Jab)", payload.MatchedMove);
        Assert.Equal("Alias", payload.MatchedBy);
        Assert.Equal("236P", payload.Motion);
    }

    [Fact]
    public async Task GetMoveQuery_WhenInputUsesStrengthQualifiedChestoAlias_ReturnsMakotoHayateVariant()
    {
        var response = await _client.GetAsync(
            "/v1/moves/query?character=makoto&moveInput=light%20chesto");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Hayate (LP)", payload.MatchedMove);
        Assert.Equal("Alias", payload.MatchedBy);
    }

    [Fact]
    public async Task GetMoveQuery_WhenMoveHasRepresentativeMedia_ReturnsMediaFields()
    {
        var response = await _client.GetAsync("/v1/moves/query?character=chun&moveInput=2mk");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(payload);
        Assert.NotNull(payload.Media);
        Assert.Equal("media/chun-li/chun-li-normals-2mk/representative-active-frame.png", payload.Media.RepresentativeFrameImageUrl);
        Assert.Equal("006", payload.Media.SelectedFrame);
        Assert.Equal("largest-active-hitbox-area", payload.Media.SelectionStrategy);
        Assert.Equal("Success", payload.Media.CaptureStatus);
        Assert.Null(payload.Media.FallbackReason);
    }

    [Fact]
    public async Task GetMoveQuery_WhenActiveDatasetChanges_ReloadsMediaMetadata()
    {
        var initialResponse = await _client.GetAsync(
            "/v1/moves/query?character=chun-li&moveInput=Kikouken%20%28Jab%29");

        Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
        var initialPayload = await initialResponse.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(initialPayload);
        Assert.Null(initialPayload.Media);

        await StaticDatasetFixtureWriter.WriteAsync(
            _datasetPath,
            StaticDatasetFixtureWriter.Character(
                "chun-li",
                "Chun-Li",
                ["chun", "chun li"],
                StaticDatasetFixtureWriter.Move("chun-li", "2mk", startup: "5"),
                StaticDatasetFixtureWriter.Move(
                    "chun-li",
                    "Kikouken (Jab)",
                    displayOrder: 2,
                    startup: "13",
                    motion: "236P",
                    damage: "60",
                    stun: "7",
                    media:
                    [
                        new StaticDatasetMoveMedia
                        {
                            Type = "RepresentativeActiveFrame",
                            Path = "media/chun-li/chun-li-normals-kikouken-jab/representative-active-frame.png",
                            SelectedFrame = "010",
                            SelectionStrategy = "largest-active-hitbox-area",
                            CaptureStatus = "Success"
                        }
                    ])));
        File.SetLastWriteTimeUtc(Path.Combine(_datasetPath, "manifest.json"), DateTime.UtcNow.AddMinutes(1));

        var reloadedResponse = await _client.GetAsync(
            "/v1/moves/query?character=chun-li&moveInput=Kikouken%20%28Jab%29");

        Assert.Equal(HttpStatusCode.OK, reloadedResponse.StatusCode);
        var reloadedPayload = await reloadedResponse.Content.ReadFromJsonAsync<MoveQueryResponse>();
        Assert.NotNull(reloadedPayload);
        Assert.NotNull(reloadedPayload.Media);
        Assert.Equal("media/chun-li/chun-li-normals-kikouken-jab/representative-active-frame.png", reloadedPayload.Media.RepresentativeFrameImageUrl);
        Assert.Equal("010", reloadedPayload.Media.SelectedFrame);
    }
}
