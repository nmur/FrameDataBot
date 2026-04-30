using FrameData.Ingestion.Services;

namespace FrameData.Ingestion.Catalog;

public sealed class SupportedCharacterCatalog : ISupportedCharacterCatalog
{
    private static readonly SourceCharacterCatalogEntry[] Characters =
    [
        Entry("alex", 1, "Alex", 1),
        Entry("ryu", 2, "Ryu", 2),
        Entry("yun", 3, "Yun", 3),
        Entry("dudley", 4, "Dudley", 4),
        Entry("necro", 5, "Necro", 5),
        Entry("hugo", 6, "Hugo", 6),
        Entry("ibuki", 7, "Ibuki", 7),
        Entry("elena", 8, "Elena", 8),
        Entry("oro", 9, "Oro", 9),
        Entry("yang", 10, "Yang", 10),
        Entry("ken", 11, "Ken", 11),
        Entry("sean", 12, "Sean", 12),
        Entry("urien", 13, "Urien", 13),
        Entry("akuma", 14, "Akuma", 14, ["gouki"]),
        Entry("gill", 15, "Gill", 15),
        Entry("chun-li", 16, "Chun-Li", 16, ["chun", "chun li", "chunli"]),
        Entry("makoto", 17, "Makoto", 17),
        Entry("q", 18, "Q", 18),
        Entry("twelve", 19, "Twelve", 19, ["12"]),
        Entry("remy", 20, "Remy", 20)
    ];

    public IReadOnlyList<SourceCharacterCatalogEntry> AllCharacters => Characters;

    public IReadOnlyList<SourceCharacterCatalogEntry> EnabledCharacters { get; } = Characters
        .Where(character => character.Enabled)
        .OrderBy(character => character.DisplayOrder)
        .ToArray();

    public IReadOnlyList<IngestionCharacterScope> ResolveScope(IReadOnlyCollection<string> characterIds)
    {
        if (characterIds.Count == 0)
        {
            return EnabledCharacters.Select(ToScope).ToArray();
        }

        return characterIds.Select(ResolveCharacter).Select(ToScope).ToArray();
    }

    private SourceCharacterCatalogEntry ResolveCharacter(string characterId)
    {
        var normalized = characterId.Trim();
        var entry = EnabledCharacters.FirstOrDefault(character =>
            string.Equals(character.Id, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(character.DisplayName, normalized, StringComparison.OrdinalIgnoreCase) ||
            character.Aliases.Any(alias => string.Equals(alias, normalized, StringComparison.OrdinalIgnoreCase)));

        return entry ?? throw new ArgumentException($"Unsupported ingestion character scope: {characterId}", nameof(characterId));
    }

    private static IngestionCharacterScope ToScope(SourceCharacterCatalogEntry entry)
        => new()
        {
            CharacterId = entry.Id,
            CharacterName = entry.DisplayName,
            SourceCharacterId = entry.SourceCharacterId,
            DisplayOrder = entry.DisplayOrder,
            Aliases = entry.Aliases
        };

    private static SourceCharacterCatalogEntry Entry(
        string id,
        int sourceCharacterId,
        string displayName,
        int displayOrder,
        IReadOnlyList<string>? aliases = null)
        => new()
        {
            Id = id,
            SourceCharacterId = sourceCharacterId,
            DisplayName = displayName,
            DisplayOrder = displayOrder,
            Aliases = aliases ?? []
        };
}
