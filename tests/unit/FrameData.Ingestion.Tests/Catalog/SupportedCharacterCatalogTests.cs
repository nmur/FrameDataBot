using FrameData.Ingestion.Catalog;
using Shouldly;

namespace FrameData.Ingestion.Tests.Catalog;

public sealed class SupportedCharacterCatalogTests
{
    private readonly SupportedCharacterCatalog _catalog = new();

    [Fact]
    public void EnabledCharacters_ContainsFullSupportedThirdStrikeCatalog()
    {
        var characters = _catalog.EnabledCharacters;

        characters.Count.ShouldBe(20);
        characters.Select(c => c.Id).ShouldContain("alex");
        characters.Select(c => c.Id).ShouldContain("chun-li");
        characters.Select(c => c.Id).ShouldContain("makoto");
        characters.Select(c => c.Id).ShouldContain("twelve");
    }

    [Fact]
    public void EnabledCharacters_HaveUniqueIdsSourceIdsAndDisplayOrder()
    {
        var characters = _catalog.EnabledCharacters;

        characters.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(characters.Count);
        characters.Select(c => c.SourceCharacterId).Distinct().Count().ShouldBe(characters.Count);
        characters.Select(c => c.DisplayOrder).Distinct().Count().ShouldBe(characters.Count);
    }

    [Fact]
    public void ResolveScope_WhenCharacterIdsProvided_ReturnsCatalogEntriesInRequestedOrder()
    {
        var scope = _catalog.ResolveScope(["makoto", "chun"]);

        scope.Count.ShouldBe(2);
        scope[0].CharacterId.ShouldBe("makoto");
        scope[0].SourceCharacterId.ShouldBe(17);
        scope[1].CharacterId.ShouldBe("chun-li");
        scope[1].SourceCharacterId.ShouldBe(16);
    }

    [Fact]
    public void ResolveScope_WhenAkumaAliasProvided_ReturnsAkuma()
    {
        var scope = _catalog.ResolveScope(["gouki"]);

        scope.Count.ShouldBe(1);
        scope[0].CharacterId.ShouldBe("akuma");
        scope[0].CharacterName.ShouldBe("Akuma");
    }

    [Fact]
    public void ResolveScope_WhenNoCharacterIdsProvided_ReturnsEnabledCatalog()
    {
        var scope = _catalog.ResolveScope([]);

        scope.Count.ShouldBe(_catalog.EnabledCharacters.Count);
        scope[0].CharacterId.ShouldBe("alex");
    }
}
