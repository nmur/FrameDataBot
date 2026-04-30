namespace FrameData.Shared.Contracts;

public sealed class MoveQueryResponse
{
    public required string Character { get; init; }
    public required string MatchedMove { get; init; }
    public required string Section { get; init; }
    public required string MatchedBy { get; init; }
    public string? Motion { get; init; }
    public string? Damage { get; init; }
    public string? Stun { get; init; }
    public string? CharacterFrameDataUrl { get; init; }
    public string? MoveHitboxDisplayUrl { get; init; }
    public required FrameDataContract FrameData { get; init; }
    public MoveMediaContract? Media { get; init; }
}

public sealed class MoveAmbiguousResponse
{
    public required string Message { get; init; }
    public required IReadOnlyList<MoveCandidate> Candidates { get; init; }
}

public sealed class MoveCandidate
{
    public required string MoveName { get; init; }
    public required string Section { get; init; }
    public required decimal Score { get; init; }
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
