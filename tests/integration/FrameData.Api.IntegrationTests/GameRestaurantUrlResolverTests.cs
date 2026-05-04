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

        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/chunli/chunli_h2.html", url);
    }

    [Theory]
    [InlineData("alex", "Alex", "Specials", "Reverse Power Bomb (Strong)", "alex_h5.html")]
    [InlineData("akuma", "Akuma", "Specials", "Zankuu Hadouken", "gouki_h2.html")]
    [InlineData("dudley", "Dudley", "Specials", "Ducking Upper", "dudley_h2.html")]
    [InlineData("hugo", "Hugo", "Specials", "Ultra Throw (RH)", "hugo_h2.html")]
    [InlineData("ibuki", "Ibuki", "Specials", "Raida (Fierce)", "ibuki_h6.html")]
    [InlineData("ken", "Ken", "Specials", "Air Tatsumaki Senpuu Kyaku (EX)", "ken_h4.html")]
    [InlineData("makoto", "Makoto", "Specials", "Karakusa (Short)", "makoto_h4.html")]
    [InlineData("necro", "Necro", "Specials", "Snake Fang (RH)", "necro_h5.html")]
    [InlineData("q", "Q", "Specials", "Capture and Deadly Blow (Forward)", "q_h5.html")]
    [InlineData("remy", "Remy", "Specials", "Light of Virtue (Short)", "remy_h2.html")]
    [InlineData("sean", "Sean", "Specials", "Dragon Smash (Fierce)", "sean_h2.html")]
    [InlineData("twelve", "Twelve", "Specials", "Air A.X.E. (Fierce)", "12_h3.html")]
    [InlineData("urien", "Urien", "Specials", "Chariot rush (RH)", "urien_h2.html")]
    [InlineData("yang", "Yang", "Specials", "Byakko Soushouda", "yang_h4.html")]
    [InlineData("yun", "Yun", "Specials", "Tetsu Zankou (Jab)", "yun_h2.html")]
    public void Resolve_WhenMoveHasCharacterSpecificSpecialPage_UsesMatchingGameRestaurantPage(
        string characterId,
        string characterName,
        string section,
        string canonicalName,
        string expectedFile)
    {
        var url = GameRestaurantUrlResolver.Resolve(Move(characterId, characterName, section, canonicalName));

        Assert.EndsWith(expectedFile, url);
    }

    [Theory]
    [InlineData("alex", "Alex", "SuperArts", "Stun Gun Headbutt", "alex_sa3.html")]
    [InlineData("akuma", "Akuma", "SuperArts", "Kongou Kokuretsu Zan", "gouki_sa5.html")]
    [InlineData("chun-li", "Chun-Li", "SuperArts", "Houyokusen", "chunli_sa2.html")]
    [InlineData("ibuki", "Ibuki", "SuperArts", "Missed grab (Chi Blast)", "ibuki_sa2.html")]
    [InlineData("oro", "Oro", "SuperArts", "Yagyou Dama", "oro_sa2.html")]
    [InlineData("q", "Q", "SuperArts", "Close Grab", "q_sa3.html")]
    [InlineData("remy", "Remy", "SuperArts", "Blue Nocturne", "remy_sa3.html")]
    [InlineData("urien", "Urien", "SuperArts", "Tyrant Slaughter", "urien_sa1.html")]
    public void Resolve_WhenMoveHasCharacterSpecificSuperArtPage_UsesMatchingGameRestaurantPage(
        string characterId,
        string characterName,
        string section,
        string canonicalName,
        string expectedFile)
    {
        var url = GameRestaurantUrlResolver.Resolve(Move(characterId, characterName, section, canonicalName));

        Assert.EndsWith(expectedFile, url);
    }

    [Fact]
    public void Resolve_WhenNormalUsesLegacyButtonName_UsesGameRestaurantButtonPageCode()
    {
        var url = GameRestaurantUrlResolver.Resolve(Move("q", "Q", "Normals", "Crouching Roundhouse"));

        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/q/q_lk.html", url);
    }

    [Fact]
    public void Resolve_WhenDirectionalNormalIsNotKnownGameRestaurantLeverMove_UsesButtonPageCode()
    {
        var url = GameRestaurantUrlResolver.Resolve(Move("necro", "Necro", "Normals", "4hp"));

        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/necro/necro_lp.html", url);
    }

    [Fact]
    public void Resolve_WhenDirectionalNormalIsKnownGameRestaurantLeverMove_UsesLeverPage()
    {
        var url = GameRestaurantUrlResolver.Resolve(Move("necro", "Necro", "Normals", "1hp"));

        Assert.Equal("http://gere.stars.ne.jp/01_3rd/kouryaku/necro/necro_lever.html", url);
    }

    [Theory]
    [InlineData("ryu", "Ryu", "Towards + Strong", "ryu_lever.html")]
    [InlineData("dudley", "Dudley", "Towards + Roundhouse", "dudley_lever.html")]
    [InlineData("ken", "Ken", "Hold Forward", "ken_mk.html")]
    [InlineData("necro", "Necro", "Back + Fierce", "necro_lp.html")]
    [InlineData("necro", "Necro", "Down Back + Fierce", "necro_lever.html")]
    public void Resolve_WhenDirectionalNormalIsAudited_UsesSpecificPageOnlyForGameRestaurantLeverMoves(
        string characterId,
        string characterName,
        string canonicalName,
        string expectedFile)
    {
        var url = GameRestaurantUrlResolver.Resolve(Move(characterId, characterName, "Normals", canonicalName));

        Assert.EndsWith(expectedFile, url);
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
