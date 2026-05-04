namespace FrameData.Domain.MoveLookup;

public enum MoveAliasType
{
    Canonical,
    Abbreviation,
    Numpad,
    Colloquial,
    Derived
}

public sealed class MoveAlias
{
    public required string Id { get; init; }
    public required string MoveId { get; init; }
    public required string Alias { get; init; }
    public required MoveAliasType AliasType { get; init; }
    public required string NormalisedAlias { get; init; }
}
