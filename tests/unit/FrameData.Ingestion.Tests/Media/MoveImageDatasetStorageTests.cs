using FrameData.Domain.Media;
using FrameData.Domain.Moves;
using FrameData.Ingestion.Media;
using FrameData.Scraper.Source;
using Shouldly;

namespace FrameData.Ingestion.Tests.Media;

public sealed class MoveImageDatasetStorageTests
{
    private readonly MoveImageDatasetStorageService _storage = new();

    [Fact]
    public void CaptureRepresentativeImage_WhenMoveInScope_StoresRelativeMediaPathAndMetadata()
    {
        var move = CreateMove("ken", "ken-normals-jab");
        const string html = """
            <div data-frame="004" data-frame-image-url="/frames/004.png">
              <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="30" data-height="2"></span>
            </div>
            """;

        var asset = _storage.CaptureRepresentativeImage(
            move,
            "http://example.test/hitboxesDisplay.php?iMove=1",
            html,
            new RepresentativeFrameSelectionPolicy
            {
                PilotMoveScope = ["ken/ken-normals-jab"]
            });

        asset.ShouldNotBeNull();
        asset.Image.StoragePath.ShouldBe("media/ken/ken-normals-jab/representative-active-frame.png");
        asset.Image.SelectedFrame.ShouldBe("004");
        asset.Image.ActiveHitboxArea.ShouldBe(60);
        asset.Image.CaptureStatus.ShouldBe(MoveImageCaptureStatus.Success);
        asset.Image.OverlayHitboxes.ShouldBe(HitboxOverlayTypes.DefaultP1Overlays);
        asset.Content[..8].ShouldBe([137, 80, 78, 71, 13, 10, 26, 10]);
    }

    [Fact]
    public async Task CaptureRepresentativeImageAsync_WhenSourceFrameImageCanBeFetched_RendersSpriteLayerBehindHitboxes()
    {
        var renderer = new HitboxCanvasRenderer();
        var sourceFramePng = CreateSourceFramePng(renderer);
        var storage = new MoveImageDatasetStorageService(sourceClient: new FakeHitboxSourceClient(sourceFramePng));
        var move = CreateMove("ken", "ken-normals-jab");
        const string html = """
            <div data-frame="004" data-frame-image-url="http://example.test/frames/004.png">
              <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="30" data-height="2"></span>
            </div>
            """;

        var asset = await storage.CaptureRepresentativeImageAsync(
            move,
            "http://example.test/hitboxesDisplay.php?iMove=1",
            html,
            new RepresentativeFrameSelectionPolicy
            {
                PilotMoveScope = ["ken/ken-normals-jab"]
            });

        asset.ShouldNotBeNull();
        renderer.TryDecodePng(asset.Content, out var rendered, out var decodeError).ShouldBeTrue(decodeError);
        rendered.ShouldNotBeNull();
        var sourcePixelOffset = ((1 * rendered.Width) + 1) * 4;
        rendered.Pixels[sourcePixelOffset].ShouldBe((byte)12);
        rendered.Pixels[sourcePixelOffset + 1].ShouldBe((byte)34);
        rendered.Pixels[sourcePixelOffset + 2].ShouldBe((byte)56);
        rendered.Pixels[sourcePixelOffset + 3].ShouldBe((byte)255);
    }

    [Fact]
    public void CaptureRepresentativeImage_WhenProjectileObjectHitboxIsActive_StoresSuccessImage()
    {
        var move = CreateMove("ken", "ken-specials-hadouken-jab");
        const string html = """
            <script>
            var sBaseUrl = 'http://example.test/repo/11/fd_normals/037/';
            aFramesInfos = {
              "010": {
                "frame": "010",
                "objects_list": [],
                "pngFileName": "010_canvas.png"
              },
              "011": {
                "frame": "011",
                "objects_list": ["OBJECT_1"],
                "pngFileName": "011_canvas.png",
                "OBJECT_1": {
                  "hitboxes": {
                    "a_hb_to_draw": [[197, 117, 123, 149]]
                  }
                }
              }
            };
            </script>
            """;

        var asset = _storage.CaptureRepresentativeImage(
            move,
            "http://example.test/hitboxesDisplay.php?iMove=37",
            html,
            new RepresentativeFrameSelectionPolicy
            {
                PilotMoveScope = ["ken/ken-specials-hadouken-jab"]
            });

        asset.ShouldNotBeNull();
        asset.Image.CaptureStatus.ShouldBe(MoveImageCaptureStatus.Success);
        asset.Image.SelectedFrame.ShouldBe("011");
        asset.Image.ActiveHitboxArea.ShouldBe(2080);
        asset.Image.SourceFrameImageUrl.ShouldBe("http://example.test/repo/11/fd_normals/037/011_canvas.png");
    }

    [Fact]
    public void RenderPng_DrawsSourceViewerStyleSolidBorderAndTransparentFill()
    {
        var renderer = new HitboxCanvasRenderer();
        var pixels = CreateSolidFramePixels(0, 0, 0);

        var content = renderer.RenderPng(
            new HitboxFrame
            {
                FrameId = "004",
                Hitboxes =
                [
                    new HitboxRectangle
                    {
                        Type = "P1_A",
                        X = 10,
                        Y = 20,
                        Width = 8,
                        Height = 8
                    }
                ]
            },
            HitboxOverlayTypes.DefaultP1Overlays,
            new DecodedPngImage(384, 224, 6, 8, pixels));

        renderer.TryDecodePng(content, out var rendered, out var decodeError).ShouldBeTrue(decodeError);
        rendered.ShouldNotBeNull();

        AssertPixel(rendered, 10, 20, 255, 0, 0);
        AssertPixel(rendered, 12, 22, 96, 0, 0);
        AssertPixel(rendered, 18, 28, 255, 0, 0);
        AssertPixel(rendered, 19, 29, 0, 0, 0);
    }

    [Fact]
    public void RenderPng_TreatsSourceViewerFuchsiaAsTransparentOverBlackBackground()
    {
        var renderer = new HitboxCanvasRenderer();
        var pixels = CreateSolidFramePixels(255, 0, 255);
        var spritePixelOffset = ((1 * 384) + 1) * 4;
        pixels[spritePixelOffset] = 12;
        pixels[spritePixelOffset + 1] = 34;
        pixels[spritePixelOffset + 2] = 56;
        pixels[spritePixelOffset + 3] = 255;

        var content = renderer.RenderPng(
            new HitboxFrame { FrameId = "004" },
            HitboxOverlayTypes.DefaultP1Overlays,
            new DecodedPngImage(384, 224, 6, 8, pixels));

        renderer.TryDecodePng(content, out var rendered, out var decodeError).ShouldBeTrue(decodeError);
        rendered.ShouldNotBeNull();
        AssertPixel(rendered, 0, 0, 0, 0, 0);
        AssertPixel(rendered, 1, 1, 12, 34, 56);
    }

    [Fact]
    public void RenderPng_UsesConfiguredHitboxPalette()
    {
        var renderer = new HitboxCanvasRenderer();
        var content = renderer.RenderPng(
            new HitboxFrame
            {
                FrameId = "004",
                Hitboxes =
                [
                    new HitboxRectangle { Type = "P1_P", X = 10, Y = 20, Width = 4, Height = 4 },
                    new HitboxRectangle { Type = "P1_V", X = 20, Y = 20, Width = 4, Height = 4 },
                    new HitboxRectangle { Type = "P1_A", X = 30, Y = 20, Width = 4, Height = 4 },
                    new HitboxRectangle { Type = "P1_T", X = 40, Y = 20, Width = 4, Height = 4 },
                    new HitboxRectangle { Type = "P1_TA", X = 50, Y = 20, Width = 4, Height = 4 }
                ]
            },
            HitboxOverlayTypes.DefaultP1Overlays,
            new DecodedPngImage(384, 224, 6, 8, CreateSolidFramePixels(0, 0, 0)));

        renderer.TryDecodePng(content, out var rendered, out var decodeError).ShouldBeTrue(decodeError);
        rendered.ShouldNotBeNull();

        AssertPixel(rendered, 10, 20, 0, 0, 255);
        AssertPixel(rendered, 20, 20, 96, 192, 255);
        AssertPixel(rendered, 30, 20, 255, 0, 0);
        AssertPixel(rendered, 40, 20, 255, 128, 0);
        AssertPixel(rendered, 50, 20, 0, 192, 0);
    }

    [Fact]
    public void CaptureRepresentativeImage_WhenMoveOutsidePilotScope_ReturnsNull()
    {
        var asset = _storage.CaptureRepresentativeImage(
            CreateMove("ken", "ken-normals-jab"),
            "http://example.test/hitboxesDisplay.php?iMove=1",
            "<div></div>",
            new RepresentativeFrameSelectionPolicy
            {
                PilotMoveScope = ["ken/ken-normals-strong"]
            });

        asset.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_WritesDummyFallbackImageToDatasetMediaTree()
    {
        var root = Path.Combine(Path.GetTempPath(), $"framedata-media-storage-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var move = CreateMove("ken", "ken-normals-jab");
            var asset = _storage.CaptureRepresentativeImage(
                move,
                "http://example.test/hitboxesDisplay.php?iMove=1",
                """
                <div data-frame="004">
                  <span data-hitbox-type="P1_A" data-x="10" data-y="20" data-width="30" data-height="2"></span>
                </div>
                """,
                new RepresentativeFrameSelectionPolicy
                {
                    PilotMoveScope = ["ken/ken-normals-jab"]
                });

            asset.ShouldNotBeNull();
            asset.Image.CaptureStatus.ShouldBe(MoveImageCaptureStatus.DummyFallback);
            asset.Image.FallbackReason.ShouldBe("Selected frame image was not available.");

            await _storage.SaveAsync(root, asset);

            File.Exists(Path.Combine(root, "media", "ken", "ken-normals-jab", "representative-active-frame.png"))
                .ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void GetRenderableHitboxes_UsesConfiguredP1OverlaysAndExcludesP2()
    {
        var renderer = new HitboxCanvasRenderer();
        var frame = new HitboxFrame
        {
            FrameId = "001",
            Hitboxes =
            [
                new HitboxRectangle { Type = "P1_P", Width = 1, Height = 1 },
                new HitboxRectangle { Type = "P1_V", Width = 1, Height = 1 },
                new HitboxRectangle { Type = "P1_A", Width = 1, Height = 1 },
                new HitboxRectangle { Type = "P1_T", Width = 1, Height = 1 },
                new HitboxRectangle { Type = "P1_TA", Width = 1, Height = 1 },
                new HitboxRectangle { Type = "P2_A", Width = 1, Height = 1 }
            ]
        };

        var renderedTypes = renderer
            .GetRenderableHitboxes(frame, HitboxOverlayTypes.DefaultP1Overlays)
            .Select(hitbox => hitbox.Type)
            .ToArray();

        renderedTypes.ShouldBe(["P1_P", "P1_V", "P1_A", "P1_T", "P1_TA"]);
    }

    private static Move CreateMove(string characterId, string moveId)
        => new()
        {
            Id = moveId,
            CharacterId = characterId,
            Game = "sf3_3s",
            CharacterName = "Ken",
            Section = "Normals",
            CanonicalName = "Jab",
            FrameData = new MoveFrameData()
        };

    private static byte[] CreateSolidFramePixels(byte red, byte green, byte blue)
    {
        var pixels = new byte[384 * 224 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = red;
            pixels[i + 1] = green;
            pixels[i + 2] = blue;
            pixels[i + 3] = 255;
        }

        return pixels;
    }

    private static void AssertPixel(DecodedPngImage image, int x, int y, byte red, byte green, byte blue)
    {
        var offset = ((y * image.Width) + x) * 4;
        image.Pixels[offset].ShouldBe(red);
        image.Pixels[offset + 1].ShouldBe(green);
        image.Pixels[offset + 2].ShouldBe(blue);
        image.Pixels[offset + 3].ShouldBe((byte)255);
    }

    private static byte[] CreateSourceFramePng(HitboxCanvasRenderer renderer)
    {
        var pixels = CreateSolidFramePixels(0, 0, 0);
        var sourcePixelOffset = ((1 * 384) + 1) * 4;
        pixels[sourcePixelOffset] = 12;
        pixels[sourcePixelOffset + 1] = 34;
        pixels[sourcePixelOffset + 2] = 56;
        pixels[sourcePixelOffset + 3] = 255;

        return renderer.RenderPng(
            new HitboxFrame { FrameId = "source" },
            [],
            new DecodedPngImage(384, 224, 6, 8, pixels));
    }

    private sealed class FakeHitboxSourceClient : IHitboxSourceClient
    {
        private readonly byte[] _sourceFramePng;

        public FakeHitboxSourceClient(byte[] sourceFramePng)
        {
            _sourceFramePng = sourceFramePng;
        }

        public Task<string> GetHitboxDisplayPageAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);

        public Task<byte[]> GetBinaryAssetAsync(string sourcePathOrUrl, CancellationToken cancellationToken = default)
            => Task.FromResult(_sourceFramePng);
    }
}
