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
                : CreateSelection(
                    overridden,
                    RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
                    CalculateActiveHitboxArea(overridden));
        }

        var strategy = moveOverride?.SelectionStrategy
            ?? policy?.DefaultStrategy
            ?? RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy;

        if (!string.Equals(
            strategy,
            RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy,
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return frames
            .Select(frame => CreateSelection(frame, RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy, CalculateActiveHitboxArea(frame)))
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
