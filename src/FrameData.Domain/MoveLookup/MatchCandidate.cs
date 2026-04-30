using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public sealed class MatchCandidate
{
    public required string MoveId { get; init; }
    public required string CanonicalName { get; init; }
    public required string Section { get; init; }
    public required string MatchedAlias { get; init; }
    public required MoveAliasType AliasType { get; init; }
    public required decimal Score { get; init; }
    public required int Rank { get; init; }
    public required bool ThresholdPassed { get; init; }
    public required Move Move { get; init; }

    public string MatchedBy => AliasType is MoveAliasType.Canonical ? "Fuzzy" : "Alias";
}
