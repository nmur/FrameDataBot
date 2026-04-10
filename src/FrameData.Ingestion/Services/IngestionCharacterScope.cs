namespace FrameData.Ingestion.Services;

public sealed class IngestionCharacterScope
{
    public required string CharacterId { get; init; }
    public required string CharacterName { get; init; }
    public int SourceCharacterId { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
}
