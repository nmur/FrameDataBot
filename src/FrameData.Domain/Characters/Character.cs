namespace FrameData.Domain.Characters;

public sealed class Character
{
    public required string Id { get; init; }
    public required string Game { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
}
