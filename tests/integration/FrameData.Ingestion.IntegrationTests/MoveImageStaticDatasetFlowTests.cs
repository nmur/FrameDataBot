using FrameData.Domain.MoveLookup;
using FrameData.Infrastructure.Dataset;
using FrameData.Ingestion.Hosting;
using FrameData.Ingestion.Media;
using FrameData.Ingestion.Publishing;
using FrameData.Ingestion.Services;
using FrameData.Scraper.Parsing;
using FrameData.Scraper.Source;

namespace FrameData.Ingestion.IntegrationTests;

public sealed class MoveImageStaticDatasetFlowTests
{
    [Fact]
    public async Task RunAsync_WhenMediaScopeIsUnrestricted_WritesMediaFilesAndMetadata()
    {
        var root = CreateTempDirectory();
        try
        {
            var options = new IngestionWorkerOptions
            {
                SourceBaseUrl = "http://example.test/index.php",
                DatasetRoot = root,
                ActiveDatasetPath = Path.Combine(root, "active")
            };
            var source = new FakeSourceHttpClient();
            var orchestrator = new IngestionOrchestrator(
                source,
                new CharacterSectionParser(),
                new StaticDatasetPublisher(new StaticDatasetPublisherOptions
                {
                    DatasetRoot = root,
                    ActiveDatasetPath = Path.Combine(root, "active")
                }),
                options,
                hitboxSourceClient: source,
                moveImageStorageService: new MoveImageDatasetStorageService(sourceClient: source));

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "ken", CharacterName = "Ken", SourceCharacterId = 14 }
            ]);

            Assert.Equal("Succeeded", run.Status);
            Assert.True(File.Exists(Path.Combine(root, "active", "media", "ken", "ken-normals-jab", "representative-active-frame.png")));
            Assert.True(File.Exists(Path.Combine(root, "active", "media", "ken", "ken-normals-strong", "representative-active-frame.png")));

            var dataset = await new StaticFrameDataDatasetLoader().LoadAsync(Path.Combine(root, "active"));
            Assert.Equal(2, dataset.MediaCount);

            var jabMedia = dataset.Moves.Single(move => move.Id == "ken-normals-jab").Media.Single();
            Assert.Equal("004", jabMedia.SelectedFrame);
            Assert.Equal("Success", jabMedia.CaptureStatus.ToString());
            Assert.Equal(60, jabMedia.ActiveHitboxArea);
            Assert.DoesNotContain("P2_A", jabMedia.OverlayHitboxes);

            var strongMedia = dataset.Moves.Single(move => move.Id == "ken-normals-strong").Media.Single();
            Assert.Equal("DummyFallback", strongMedia.CaptureStatus.ToString());
            Assert.Equal("Selected frame image was not available.", strongMedia.FallbackReason);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [Fact]
    public async Task RunAsync_WhenOroCrouchingRoundhouseIsIngested_AddsPeanutMoveWithFrameTwentyTwoMedia()
    {
        var root = CreateTempDirectory();
        try
        {
            var options = new IngestionWorkerOptions
            {
                SourceBaseUrl = "http://example.test/index.php",
                DatasetRoot = root,
                ActiveDatasetPath = Path.Combine(root, "active")
            };
            var source = new OroSourceHttpClient();
            var orchestrator = new IngestionOrchestrator(
                source,
                new CharacterSectionParser(),
                new StaticDatasetPublisher(new StaticDatasetPublisherOptions
                {
                    DatasetRoot = root,
                    ActiveDatasetPath = Path.Combine(root, "active")
                }),
                options,
                hitboxSourceClient: source,
                moveImageStorageService: new MoveImageDatasetStorageService(sourceClient: source));

            var run = await orchestrator.RunAsync(
            [
                new IngestionCharacterScope { CharacterId = "oro", CharacterName = "Oro", SourceCharacterId = 9 }
            ]);

            Assert.Equal("Succeeded", run.Status);

            var dataset = await new StaticFrameDataDatasetLoader().LoadAsync(Path.Combine(root, "active"));
            var customMove = dataset.Moves.Single(move => move.Id == "oro-custom-peanut");
            Assert.Equal("Indecent Exposure", customMove.CanonicalName);
            Assert.Equal("Specials", customMove.Section);
            Assert.Equal("69", customMove.Damage);
            Assert.Equal("8", customMove.FrameData.Startup);
            Assert.Equal("4", customMove.FrameData.Active);
            Assert.Equal("20", customMove.FrameData.Recovery);

            var media = customMove.Media.Single();
            Assert.Equal("022", media.SelectedFrame);
            Assert.Equal("Success", media.CaptureStatus.ToString());
            Assert.Equal("media/oro/oro-custom-peanut/representative-active-frame.png", media.StoragePath);
            Assert.Empty(media.OverlayHitboxes);
            Assert.True(File.Exists(Path.Combine(root, "active", media.StoragePath.Replace('/', Path.DirectorySeparatorChar))));

            var repository = new StaticMoveQueryRepository(dataset);
            var lookupService = new ExactMoveLookupService(repository);
            var result = await lookupService.LookupAsync("oro", "🥜");
            Assert.True(result.IsFound);
            Assert.Equal("Alias", result.MatchedBy);
            Assert.NotNull(result.Move);
            Assert.Equal("oro-custom-peanut", result.Move.Id);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"framedata-media-flow-{Guid.NewGuid():N}");
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

    private sealed class FakeSourceHttpClient : ISourceHttpClient, IHitboxSourceClient
    {
        public Task<string> GetCharacterPageAsync(int sourceCharacterId, CancellationToken cancellationToken = default)
            => Task.FromResult("""
                <html><body>
                  <h2>Normals</h2>
                  <table>
                    <tr><th></th><th>Name</th><th>Startup</th><th>Hit</th><th>Recovery</th></tr>
                    <tr>
                      <td title="1"><div class="linkHitboxes" id="load_1"><div class="none">00001</div></div></td>
                      <td>Jab</td>
                      <td>4</td><td>2</td><td>9</td>
                    </tr>
                    <tr>
                      <td title="2"><div class="linkHitboxes" id="load_2"><div class="none">00002</div></div></td>
                      <td>Strong</td>
                      <td>5</td><td>3</td><td>12</td>
                    </tr>
                  </table>
                </body></html>
                """);

        public Task<string> GetHitboxDisplayPageAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default)
        {
            if (sourcePathOrUrl.Contains("iMove=2", StringComparison.Ordinal))
            {
                return Task.FromResult("""
                    <div data-frame="003">
                      <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="5" data-height="5"></span>
                    </div>
                    """);
            }

            return Task.FromResult("""
                <div data-frame="003" data-frame-image-url="/frames/003.png">
                  <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="3" data-height="3"></span>
                </div>
                <div data-frame="004" data-frame-image-url="/frames/004.png">
                  <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="30" data-height="2"></span>
                  <span data-hitbox-type="P2_A" data-x="0" data-y="0" data-width="200" data-height="200"></span>
                </div>
                """);
        }

        public Task<byte[]> GetBinaryAssetAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(new HitboxCanvasRenderer().RenderDummyPng());
    }

    private sealed class OroSourceHttpClient : ISourceHttpClient, IHitboxSourceClient
    {
        public Task<string> GetCharacterPageAsync(int sourceCharacterId, CancellationToken cancellationToken = default)
            => Task.FromResult("""
                <html><body>
                  <h2>Normals</h2>
                  <table>
                    <tr><th></th><th>Name</th><th>Startup</th><th>Hit</th><th>Recovery</th><th>Damage</th><th>Stun</th></tr>
                    <tr>
                      <td title="22"><div class="linkHitboxes" id="load_22"><div class="none">00022</div></div></td>
                      <td>Crouching Roundhouse</td>
                      <td>8</td><td>4</td><td>20</td><td>80</td><td>13</td>
                    </tr>
                  </table>
                </body></html>
                """);

        public Task<string> GetHitboxDisplayPageAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default)
            => Task.FromResult("""
                <div data-frame="021" data-frame-image-url="http://example.test/frames/021.png">
                  <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="200" data-height="1"></span>
                </div>
                <div data-frame="022" data-frame-image-url="http://example.test/frames/022.png">
                  <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="5" data-height="5"></span>
                </div>
                """);

        public Task<byte[]> GetBinaryAssetAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(new HitboxCanvasRenderer().RenderDummyPng());
    }
}
