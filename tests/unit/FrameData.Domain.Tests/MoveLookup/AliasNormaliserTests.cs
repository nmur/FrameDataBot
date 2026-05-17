using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;
using Shouldly;

namespace FrameData.Domain.Tests.MoveLookup;

public sealed class AliasNormaliserTests
{
    private readonly AliasNormaliser _normaliser = new();

    [Theory]
    [InlineData("cr.HK", "2hk")]
    [InlineData("crouching Heavy Kick", "2hk")]
    [InlineData("2HK", "2hk")]
    [InlineData("Down Back + Fierce", "1hp")]
    [InlineData("down + back Fierce", "1hp")]
    [InlineData("down-back HP", "1hp")]
    [InlineData("db.HP", "1hp")]
    [InlineData("d/b fierce", "1hp")]
    [InlineData("Down Forward + Fierce", "3hp")]
    [InlineData("down + forward HP", "3hp")]
    [InlineData("down-forward HP", "3hp")]
    [InlineData("df.HP", "3hp")]
    [InlineData("d/f fierce", "3hp")]
    [InlineData("Towards + Forward", "6mk")]
    [InlineData("towards MK", "6mk")]
    [InlineData("toward roundhouse", "6hk")]
    [InlineData("f.HP", "6hp")]
    [InlineData("f+HP", "6hp")]
    [InlineData("Back + Fierce", "4hp")]
    [InlineData("back HP", "4hp")]
    [InlineData("b.HK", "4hk")]
    [InlineData("b+HK", "4hk")]
    [InlineData("low forward", "2mk")]
    [InlineData("Standing LP", "5lp")]
    [InlineData("standing HP", "5hp")]
    [InlineData("st.HK", "5hk")]
    [InlineData("st.MP", "5mp")]
    [InlineData("RH", "hk")]
    [InlineData("Air Knee Smash RH", "jkneesmashhk")]
    [InlineData("jumping Heavy Punch", "jhp")]
    [InlineData("air Heavy Punch", "jhp")]
    [InlineData("jp.HP", "jhp")]
    [InlineData("jp tatsu", "jtatsu")]
    [InlineData("j.HK", "jhk")]
    [InlineData("QCF + Jab", "236lp")]
    [InlineData("quarter circle forward LP", "236lp")]
    [InlineData("236 + LP", "236lp")]
    [InlineData("QCB + Short", "214lk")]
    [InlineData("quarter circle back LK", "214lk")]
    [InlineData("HCF + Fierce", "41236hp")]
    [InlineData("half circle forward HP", "41236hp")]
    [InlineData("HCB + Roundhouse", "63214hk")]
    [InlineData("half circle back HK", "63214hk")]
    [InlineData("360", "360")]
    [InlineData("360 + LP", "360lp")]
    [InlineData("QCF x 2", "236236")]
    [InlineData("236236", "236236")]
    [InlineData("double quarter circle forward", "236236")]
    [InlineData("double qcf", "236236")]
    [InlineData("2x qcf", "236236")]
    [InlineData("2x quarter circle forward", "236236")]
    [InlineData("QCB x 2", "214214")]
    [InlineData("214214", "214214")]
    [InlineData("double quarter circle back", "214214")]
    [InlineData("double qcb", "214214")]
    [InlineData("2x qcb", "214214")]
    [InlineData("2x quarter circle back", "214214")]
    [InlineData("DPM + Forward", "dpmmk")]
    [InlineData("dp mk", "dpmmk")]
    [InlineData("dp", "dp")]
    [InlineData("D.D.T.", "ddt")]
    [InlineData("stomp", "stomp")]
    [InlineData("backbreaker", "backbreaker")]
    [InlineData("crab punch", "crabpunch")]
    [InlineData("🥜", "🥜")]
    public void Normalise_ConvertsCommonNotationToStableLookupForm(string input, string expected)
    {
        var normalised = _normaliser.Normalise(input);

        normalised.ShouldBe(expected);
    }

    [Fact]
    public void Normalise_WhenInputIsColloquial_PreservesSearchableTerm()
    {
        var normalised = _normaliser.Normalise(" sweep ");

        normalised.ShouldBe("sweep");
    }

    [Fact]
    public void CreateAliases_WhenCanonicalMoveUsesBackNumpadNotation_AddsBackAbbreviationAlias()
    {
        var move = new Move
        {
            Id = "test-4hp",
            CharacterId = "test",
            Game = "sf3_3s",
            CharacterName = "Test",
            Section = "Normals",
            CanonicalName = "4hp",
            DisplayOrder = 1,
            FrameData = new MoveFrameData()
        };

        var aliases = _normaliser.CreateAliases(move);

        aliases.ShouldContain(alias => alias.Alias == "b.hp" && alias.NormalisedAlias == "4hp");
    }

    [Fact]
    public void CreateAliases_WhenCanonicalMoveUsesForwardNumpadNotation_AddsForwardAbbreviationAlias()
    {
        var move = new Move
        {
            Id = "test-6hp",
            CharacterId = "test",
            Game = "sf3_3s",
            CharacterName = "Test",
            Section = "Normals",
            CanonicalName = "6hp",
            DisplayOrder = 1,
            FrameData = new MoveFrameData()
        };

        var aliases = _normaliser.CreateAliases(move);

        aliases.ShouldContain(alias => alias.Alias == "f.hp" && alias.NormalisedAlias == "6hp");
    }

    [Fact]
    public void CreateAliases_WhenOroCustomPeanutMove_AddsPeanutAlias()
    {
        var move = new Move
        {
            Id = "oro-custom-peanut",
            CharacterId = "oro",
            Game = "sf3_3s",
            CharacterName = "Oro",
            Section = "Specials",
            CanonicalName = "Indecent Exposure",
            DisplayOrder = 1,
            FrameData = new MoveFrameData()
        };

        var aliases = _normaliser.CreateAliases(move);

        aliases.ShouldContain(alias => alias.Alias == "🥜" && alias.NormalisedAlias == "🥜");
    }
}
