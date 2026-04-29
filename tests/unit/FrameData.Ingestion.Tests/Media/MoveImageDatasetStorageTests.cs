using FrameData.Domain.Media;
using FrameData.Domain.Moves;
using FrameData.Ingestion.Media;
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
}
