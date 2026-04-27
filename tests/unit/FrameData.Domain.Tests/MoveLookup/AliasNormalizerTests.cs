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
    [InlineData("low forward", "2mk")]
    [InlineData("Standing LP", "5lp")]
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
