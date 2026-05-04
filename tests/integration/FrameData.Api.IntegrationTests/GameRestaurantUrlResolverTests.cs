using FrameData.Api.Responses;
using FrameData.Domain.Moves;

namespace FrameData.Api.IntegrationTests;

public sealed class GameRestaurantUrlResolverTests
{
    [Fact]
    public void Resolve_WhenCharacterUsesDifferentGameRestaurantSiteKey_UsesSiteKeyForFolderAndFilePrefix()
    {
        var url = GameRestaurantUrlResolver.Resolve(Move("akuma", "Akuma", "Normals", "Jab"));

        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/gouki/gouki_sp.html", url);
    }

    [Fact]
    public void Resolve_WhenCharacterIsResolvedBySiteKey_UsesCanonicalCharacterMovePageMappings()
    {
        var url = GameRestaurantUrlResolver.Resolve(Move("chunli", "Chun Li", "Specials", "Kikouken (Jab)"));

        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/chunli/chunli_h1.html", url);
    }

    [Fact]
    public void Resolve_WhenNormalUsesLegacyButtonName_UsesGameRestaurantButtonPageCode()
    {
        var url = GameRestaurantUrlResolver.Resolve(Move("q", "Q", "Normals", "Crouching Roundhouse"));

        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/q/q_lk.html", url);
    }

    private static Move Move(string characterId, string characterName, string section, string canonicalName)
        => new()
        {
            Id = $"{characterId}-{canonicalName}",
            CharacterId = characterId,
            Game = "sf3_3s",
            CharacterName = characterName,
            Section = section,
            CanonicalName = canonicalName,
            FrameData = new MoveFrameData()
        };
}
