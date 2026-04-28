using FrameData.Domain.MoveLookup;
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
    [InlineData("Back + Fierce", "5hp")]
    [InlineData("back HP", "5hp")]
    [InlineData("low forward", "2mk")]
    [InlineData("Standing LP", "5lp")]
    [InlineData("st.HK", "5hk")]
    [InlineData("jumping Heavy Punch", "jhp")]
    [InlineData("air Heavy Punch", "jhp")]
    [InlineData("jp.HP", "jhp")]
    [InlineData("jp tatsu", "jtatsu")]
    [InlineData("j.HK", "jhk")]
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
}
