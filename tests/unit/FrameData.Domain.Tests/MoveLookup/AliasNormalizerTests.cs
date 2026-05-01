using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;
using Shouldly;

namespace FrameData.Domain.Tests.MoveLookup;

public sealed class AliasNormalizerTests
{
    private readonly AliasNormalizer _normalizer = new();

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
    [InlineData("Back + Fierce", "4hp")]
    [InlineData("back HP", "4hp")]
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
    [InlineData("stomp", "stomp")]
    [InlineData("backbreaker", "backbreaker")]
    [InlineData("crab punch", "crabpunch")]
    public void Normalize_ConvertsCommonNotationToStableLookupForm(string input, string expected)
    {
        var normalized = _normalizer.Normalize(input);

        normalized.ShouldBe(expected);
    }

    [Fact]
    public void Normalize_WhenInputIsColloquial_PreservesSearchableTerm()
    {
        var normalized = _normalizer.Normalize(" sweep ");

        normalized.ShouldBe("sweep");
    }

    [Fact]
    public void CreateAliases_WhenCanonicalMoveUsesBackNumpadNotation_AddsBackAlias()
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

        var aliases = _normalizer.CreateAliases(move);

        aliases.ShouldContain(alias => alias.Alias == "back hp" && alias.NormalizedAlias == "4hp");
    }
}
