namespace FrameData.Domain.Media;

public sealed class RepresentativeFrameSelector
{
    public RepresentativeFrameSelection? Select(
        IReadOnlyCollection<HitboxFrame> frames,
        RepresentativeFrameSelectionPolicy? policy = null,
        RepresentativeFrameSelectionOverride? moveOverride = null)
    {
        if (frames.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(moveOverride?.SelectedFrame))
        {
            var overridden = frames
                .OrderBy(frame => FrameSortKey(frame.FrameId))
                .FirstOrDefault(frame => string.Equals(
                    frame.FrameId,
                    moveOverride.SelectedFrame,
                    StringComparison.OrdinalIgnoreCase));

            return overridden is null
                ? null
                : CreateSelectionForOverriddenFrame(overridden);
        }

        var strategy = moveOverride?.SelectionStrategy
            ?? policy?.DefaultStrategy
            ?? RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy;

        if (string.Equals(
                strategy,
                RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
                StringComparison.OrdinalIgnoreCase))
        {
            return SelectLargestActiveHitboxArea(frames)
                ?? SelectLargestActiveThrowHitboxArea(frames);
        }

        if (string.Equals(
                strategy,
                RepresentativeFrameSelectionPolicy.LargestActiveThrowHitboxAreaStrategy,
                StringComparison.OrdinalIgnoreCase))
        {
            return SelectLargestActiveThrowHitboxArea(frames);
        }

        return null;
    }

    private static RepresentativeFrameSelection? SelectLargestActiveHitboxArea(IReadOnlyCollection<HitboxFrame> frames)
        => SelectLargestArea(
            frames,
            RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
            CalculateActiveHitboxArea);

    private static RepresentativeFrameSelection? SelectLargestActiveThrowHitboxArea(IReadOnlyCollection<HitboxFrame> frames)
        => SelectLargestArea(
            frames,
            RepresentativeFrameSelectionPolicy.LargestActiveThrowHitboxAreaStrategy,
            CalculateActiveThrowHitboxArea);

    private static RepresentativeFrameSelection? SelectLargestArea(
        IReadOnlyCollection<HitboxFrame> frames,
        string strategy,
        Func<HitboxFrame, int> calculateArea)
    {
        return frames
            .Select(frame => CreateSelection(frame, strategy, calculateArea(frame)))
            .Where(selection => selection.ActiveHitboxArea > 0)
            .OrderByDescending(selection => selection.ActiveHitboxArea)
            .ThenBy(selection => FrameSortKey(selection.Frame.FrameId))
            .ThenBy(selection => selection.Frame.FrameId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public static int CalculateActiveHitboxArea(HitboxFrame frame)
        => frame.Hitboxes
            .Where(hitbox => HitboxOverlayTypes.IsActiveAreaHitbox(hitbox.Type))
            .Sum(hitbox => hitbox.Area);

    public static int CalculateActiveThrowHitboxArea(HitboxFrame frame)
        => frame.Hitboxes
            .Where(hitbox => HitboxOverlayTypes.IsActiveThrowHitbox(hitbox.Type))
            .Sum(hitbox => hitbox.Area);

    private static RepresentativeFrameSelection CreateSelectionForOverriddenFrame(HitboxFrame frame)
    {
        var activeHitboxArea = CalculateActiveHitboxArea(frame);
        if (activeHitboxArea > 0)
        {
            return CreateSelection(
                frame,
                RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
                activeHitboxArea);
        }

        var activeThrowHitboxArea = CalculateActiveThrowHitboxArea(frame);
        if (activeThrowHitboxArea > 0)
        {
            return CreateSelection(
                frame,
                RepresentativeFrameSelectionPolicy.LargestActiveThrowHitboxAreaStrategy,
                activeThrowHitboxArea);
        }

        return CreateSelection(
            frame,
            RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
            activeHitboxArea);
    }

    private static RepresentativeFrameSelection CreateSelection(
        HitboxFrame frame,
        string strategy,
        int activeHitboxArea)
        => new()
        {
            Frame = frame,
            SelectionStrategy = strategy,
            ActiveHitboxArea = activeHitboxArea
        };

    private static int FrameSortKey(string frameId)
        => int.TryParse(frameId, out var parsed) ? parsed : int.MaxValue;
}

public sealed class RepresentativeFrameSelection
{
    public required HitboxFrame Frame { get; init; }
    public required string SelectionStrategy { get; init; }
    public int ActiveHitboxArea { get; init; }
}
