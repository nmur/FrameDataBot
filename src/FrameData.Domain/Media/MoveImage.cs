namespace FrameData.Domain.Media;

public sealed class MoveImage
{
    public required string Id { get; init; }
    public required string MoveId { get; init; }
    public MoveImageType ImageType { get; init; } = MoveImageType.RepresentativeActiveFrame;
    public required string StoragePath { get; init; }
    public required string SourceUrl { get; init; }
    public string? SourceFrameImageUrl { get; init; }
    public string? SelectedFrame { get; init; }
    public string SelectionStrategy { get; init; } = RepresentativeFrameSelectionPolicy.LargestActiveHitboxAreaStrategy;
    public int? ActiveHitboxArea { get; init; }
    public IReadOnlyList<string> OverlayHitboxes { get; init; } = HitboxOverlayTypes.DefaultP1Overlays;
    public string? FallbackReason { get; init; }
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
    public MoveImageCaptureStatus CaptureStatus { get; init; } = MoveImageCaptureStatus.Success;
}

public enum MoveImageType
{
    RepresentativeActiveFrame,
    Other
}

public enum MoveImageCaptureStatus
{
    Success,
    DummyFallback,
    Failed,
    NotDerivable
}
