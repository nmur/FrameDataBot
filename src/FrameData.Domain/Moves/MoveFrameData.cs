namespace FrameData.Domain.Moves;

public sealed class MoveFrameData
{
    public string? Startup { get; init; }
    public string? Active { get; init; }
    public string? Recovery { get; init; }
    public string? OnHit { get; init; }
    public string? OnBlock { get; init; }
    public string? FrameAdvantage { get; init; }
    public string? Notes { get; init; }
}
