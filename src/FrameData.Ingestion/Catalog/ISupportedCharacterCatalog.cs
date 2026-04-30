using FrameData.Ingestion.Services;

namespace FrameData.Ingestion.Catalog;

public interface ISupportedCharacterCatalog
{
    IReadOnlyList<SourceCharacterCatalogEntry> AllCharacters { get; }
    IReadOnlyList<SourceCharacterCatalogEntry> EnabledCharacters { get; }
    IReadOnlyList<IngestionCharacterScope> ResolveScope(IReadOnlyCollection<string> characterIds);
}
