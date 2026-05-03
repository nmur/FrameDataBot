using FrameData.Domain.Media;
using Shouldly;

namespace FrameData.Domain.Tests.Media;

public sealed class RepresentativeFrameSelectorTests
{
    private readonly RepresentativeFrameSelector _selector = new();

    [Fact]
    public void Select_ChoosesEarliestFrameWithLargestSummedActiveArea()
    {
        var frames = new[]
        {
            Frame("007", Box("P1_A", width: 10, height: 10)),
            Frame("005", Box("P1_A", width: 20, height: 5)),
            Frame("006", Box("P1_A", width: 5, height: 5))
        };

        var selected = _selector.Select(frames);

        selected.ShouldNotBeNull();
        selected.Frame.FrameId.ShouldBe("005");
        selected.ActiveHitboxArea.ShouldBe(100);
        selected.SelectionStrategy.ShouldBe(RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy);
    }

    [Fact]
    public void Select_IncludesObjectActiveHitboxesAndExcludesP2Hitboxes()
    {
        var frames = new[]
        {
            Frame(
                "001",
                Box("P2_A", width: 100, height: 100),
                Box("OBJ_A", width: 8, height: 9)),
            Frame("002", Box("P1_V", width: 100, height: 100))
        };

        var selected = _selector.Select(frames);

        selected.ShouldNotBeNull();
        selected.Frame.FrameId.ShouldBe("001");
        selected.ActiveHitboxArea.ShouldBe(72);
    }

    [Fact]
    public void Select_WhenNoActiveHitboxes_ChoosesEarliestFrameWithLargestSummedActiveThrowArea()
    {
        var frames = new[]
        {
            Frame("006", Box("P1_T", width: 10, height: 10)),
            Frame("004", Box("P1_T", width: 20, height: 5)),
            Frame("005", Box("P1_T", width: 5, height: 5)),
            Frame("003", Box("P2_T", width: 100, height: 100))
        };

        var selected = _selector.Select(frames);

        selected.ShouldNotBeNull();
        selected.Frame.FrameId.ShouldBe("004");
        selected.ActiveHitboxArea.ShouldBe(100);
        selected.SelectionStrategy.ShouldBe(RepresentativeFrameSelectionPolicy.LargestActiveThrowHitboxAreaStrategy);
    }

    [Fact]
    public void Select_WhenActiveAndActiveThrowHitboxesExist_DefaultStrategyPrefersActiveHitboxes()
    {
        var frames = new[]
        {
            Frame("001", Box("P1_A", width: 2, height: 2)),
            Frame("002", Box("P1_T", width: 20, height: 20))
        };

        var selected = _selector.Select(frames);

        selected.ShouldNotBeNull();
        selected.Frame.FrameId.ShouldBe("001");
        selected.ActiveHitboxArea.ShouldBe(4);
        selected.SelectionStrategy.ShouldBe(RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy);
    }

    [Fact]
    public void Select_WhenThrowStrategyConfigured_ChoosesLargestActiveThrowArea()
    {
        var frames = new[]
        {
            Frame("001", Box("P1_A", width: 50, height: 50)),
            Frame("002", Box("P1_T", width: 20, height: 20))
        };

        var selected = _selector.Select(
            frames,
            new RepresentativeFrameSelectionPolicy
            {
                DefaultStrategy = RepresentativeFrameSelectionPolicy.LargestActiveThrowHitboxAreaStrategy
            });

        selected.ShouldNotBeNull();
        selected.Frame.FrameId.ShouldBe("002");
        selected.ActiveHitboxArea.ShouldBe(400);
        selected.SelectionStrategy.ShouldBe(RepresentativeFrameSelectionPolicy.LargestActiveThrowHitboxAreaStrategy);
    }

    [Fact]
    public void Select_WhenOverrideSpecifiesFrame_UsesThatFrame()
    {
        var frames = new[]
        {
            Frame("001", Box("P1_A", width: 50, height: 50)),
            Frame("009", Box("P1_A", width: 1, height: 1))
        };

        var selected = _selector.Select(
            frames,
            moveOverride: new RepresentativeFrameSelectionOverride { SelectedFrame = "009" });

        selected.ShouldNotBeNull();
        selected.Frame.FrameId.ShouldBe("009");
        selected.ActiveHitboxArea.ShouldBe(1);
    }

    private static HitboxFrame Frame(string frameId, params HitboxRectangle[] hitboxes)
        => new()
        {
            FrameId = frameId,
            SourceFrameImageUrl = $"/frames/{frameId}.png",
            Hitboxes = hitboxes
        };

    private static HitboxRectangle Box(string type, int width, int height)
        => new()
        {
            Type = type,
            Width = width,
            Height = height
        };
}
