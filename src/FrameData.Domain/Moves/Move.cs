namespace FrameData.Domain.Moves;

public sealed class Move
{
    public required string Id { get; init; }
    public required string CharacterId { get; init; }
    public required string Game { get; init; }
    public required string CharacterName { get; init; }
    public required string Section { get; init; }
    public required string CanonicalName { get; init; }
    public int? DisplayOrder { get; init; }
    public string? SourceMoveId { get; init; }
    public required MoveFrameData FrameData { get; init; }
}
