namespace FrameData.Domain.Characters;

public sealed class Character
{
    public required string Id { get; init; }
    public required string Game { get; init; }
    public required string Name { get; init; }
    public int? SourceCharacterId { get; init; }
    public int DisplayOrder { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
}
