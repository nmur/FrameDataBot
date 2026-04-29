namespace FrameData.Shared.Contracts;

public sealed class MoveMediaContract
{
    public string? RepresentativeFrameImageUrl { get; init; }
    public string? SelectedFrame { get; init; }
    public string? SelectionStrategy { get; init; }
    public string? CaptureStatus { get; init; }
    public string? FallbackReason { get; init; }
}
