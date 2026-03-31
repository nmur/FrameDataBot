namespace FrameData.Shared.Contracts;

public sealed class MoveQueryRequest
{
    public string? Game { get; init; }
    public required string Character { get; init; }
    public required string MoveInput { get; init; }
    public bool AllowFuzzy { get; init; } = true;
}

public sealed class MoveQueryResponse
{
    public required string Character { get; init; }
    public required string MatchedMove { get; init; }
    public required string Section { get; init; }
    public required string MatchedBy { get; init; }
    public required FrameDataContract FrameData { get; init; }
}

public sealed class FrameDataContract
{
    public string? Startup { get; init; }
    public string? Active { get; init; }
    public string? Recovery { get; init; }
    public string? OnHit { get; init; }
    public string? OnBlock { get; init; }
    public string? FrameAdvantage { get; init; }
}

public sealed class ErrorResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}
