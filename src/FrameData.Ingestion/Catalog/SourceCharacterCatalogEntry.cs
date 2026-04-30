namespace FrameData.Ingestion.Catalog;

public sealed class SourceCharacterCatalogEntry
{
    public required string Id { get; init; }
    public int SourceCharacterId { get; init; }
    public required string DisplayName { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public bool Enabled { get; init; } = true;
    public int DisplayOrder { get; init; }
}
