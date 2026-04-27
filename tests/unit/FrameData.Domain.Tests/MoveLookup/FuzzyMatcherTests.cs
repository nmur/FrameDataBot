using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;
using Shouldly;

namespace FrameData.Domain.Tests.MoveLookup;

public sealed class FuzzyMatcherTests
{
    private readonly FuzzyMoveMatcher _matcher = new(new AliasNormalizer());

    [Fact]
    public void Rank_WhenInputUsesCrouchingNotation_RanksNumpadCanonicalMoveFirst()
    {
        var candidates = _matcher.Rank("cr.HK", CreateMoves());

        candidates[0].CanonicalName.ShouldBe("2hk");
        candidates[0].MatchedAlias.ShouldBe("2hk");
        candidates[0].ThresholdPassed.ShouldBeTrue();
        candidates[0].Score.ShouldBe(100);
    }

    [Fact]
    public void Rank_WhenInputUsesColloquialAlias_RanksDerivedMoveFirst()
    {
        var candidates = _matcher.Rank("sweep", CreateMoves());

        candidates[0].CanonicalName.ShouldBe("2hk");
        candidates[0].MatchedAlias.ShouldBe("sweep");
        candidates[0].ThresholdPassed.ShouldBeTrue();
        candidates[0].Score.ShouldBe(100);
    }

    [Theory]
    [InlineData("kanipan", "Dudley", "2hk")]
    [InlineData("chesto", "Makoto", "Hayate")]
    public void Rank_WhenInputUsesMoveSpecificColloquialAlias_RanksConfiguredMoveFirst(
        string input,
        string expectedCharacter,
        string expectedMove)
    {
        var candidates = _matcher.Rank(input, CreateMoveSpecificColloquialMoves());

        candidates[0].CanonicalName.ShouldBe(expectedMove);
        candidates[0].Move.CharacterName.ShouldBe(expectedCharacter);
        candidates[0].MatchedAlias.ShouldBe(input);
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Fact]
    public void Rank_WhenDirectionalInputIsMoreSpecific_ItScoresHigherThanButtonOnlyInput()
    {
        var moves = new[]
        {
            new Move
            {
                Id = "makoto-2hp",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "2hp",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "7" }
            }
        };

        var specific = _matcher.Rank("crouching fierce", moves);
        var generic = _matcher.Rank("fierce", moves);

        specific[0].CanonicalName.ShouldBe("2hp");
        specific[0].Score.ShouldBe(100);
        generic[0].CanonicalName.ShouldBe("2hp");
        generic[0].Score.ShouldBeLessThan(specific[0].Score);
    }

    [Fact]
    public void Rank_WhenInputIsLeadingChunkOfCanonicalMoveName_ScoresAsHighConfidencePrefixMatch()
    {
        var moves = new[]
        {
            new Move
            {
                Id = "akuma-kongou-kokuretsu-zan",
                CharacterId = "akuma",
                Game = "sf3_3s",
                CharacterName = "Akuma",
                Section = "SuperArts",
                CanonicalName = "Kongou Kokuretsu Zan",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "1" }
            }
        };

        var candidates = _matcher.Rank("kongou", moves);

        candidates[0].CanonicalName.ShouldBe("Kongou Kokuretsu Zan");
        candidates[0].MatchedAlias.ShouldBe("kongoukokuretsuzan");
        candidates[0].Score.ShouldBe(96);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("light hadouken", "Hadouken (Jab)", "lighthadouken")]
    [InlineData("light tatsumaki senpuu kyaku", "Tatsumaki Senpuu Kyaku (Short)", "lighttatsumakisenpuukyaku")]
    public void Rank_WhenSpecialMoveHasParenthesizedNormal_GeneratesStrengthAliases(
        string input,
        string expectedMove,
        string expectedAlias)
    {
        var candidates = _matcher.Rank(input, CreateSpecialMoves());

        candidates[0].CanonicalName.ShouldBe(expectedMove);
        candidates[0].MatchedAlias.ShouldBe(expectedAlias);
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("jp jab", "Jumping Jab", "jlp")]
    [InlineData("air jab", "Jumping Jab", "jlp")]
    [InlineData("jumping fierce", "Jumping Fierce", "jhp")]
    public void Rank_WhenInputUsesJumpNotation_RanksJumpingMoveFirst(
        string input,
        string expectedMove,
        string expectedAlias)
    {
        var candidates = _matcher.Rank(input, CreateNormalVariantMoves());

        candidates[0].CanonicalName.ShouldBe(expectedMove);
        candidates[0].MatchedAlias.ShouldBe(expectedAlias);
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("air tatsu")]
    [InlineData("jp tatsu")]
    public void Rank_WhenInputUsesAirSpecialShortName_RanksAirSpecialMoveFirst(string input)
    {
        var candidates = _matcher.Rank(input, CreateSpecialMoves());

        candidates[0].CanonicalName.ShouldBe("Air Tatsumaki Senpuu Kyaku (Short)");
        candidates[0].MatchedAlias.ShouldBe("jtatsu");
        candidates[0].Score.ShouldBe(100);
    }

    [Fact]
    public void Rank_WhenInputUsesUniversalOverheadInitialism_RanksUniversalOverheadFirst()
    {
        var candidates = _matcher.Rank("uoh", CreateSpecialMoves());

        candidates[0].CanonicalName.ShouldBe("Universal Overhead");
        candidates[0].MatchedAlias.ShouldBe("uoh");
        candidates[0].Score.ShouldBe(100);
    }

    [Fact]
    public void Rank_WhenInputUsesTatsuShortName_RanksTatsumakiFirst()
    {
        var candidates = _matcher.Rank("tatsu", CreateSpecialMoves());

        candidates[0].CanonicalName.ShouldBe("Tatsumaki Senpuu Kyaku (Short)");
        candidates[0].MatchedAlias.ShouldBe("tatsu");
        candidates[0].Score.ShouldBe(100);
    }

    [Theory]
    [InlineData("shipu")]
    [InlineData("shippu")]
    public void Rank_WhenInputUsesShippuShortName_RanksShipuujinraiFirst(string input)
    {
        var candidates = _matcher.Rank(input, CreateSpecialMoves());

        candidates[0].CanonicalName.ShouldBe("Shipuujinrai Kyaku");
        candidates[0].MatchedAlias.ShouldBe(input);
        candidates[0].Score.ShouldBe(100);
    }

    [Theory]
    [InlineData("close jab")]
    [InlineData("cl jab")]
    public void Rank_WhenInputUsesCloseNotation_RanksNonFarNormalFirst(string input)
    {
        var candidates = _matcher.Rank(input, CreateNormalVariantMoves());

        candidates[0].CanonicalName.ShouldBe("Jab");
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Fact]
    public void Rank_WhenInputUsesFarNotation_RanksFarNormalFirst()
    {
        var candidates = _matcher.Rank("far jab", CreateNormalVariantMoves());

        candidates[0].CanonicalName.ShouldBe("Far Jab");
        candidates[0].MatchedAlias.ShouldBe("farlp");
        candidates[0].Score.ShouldBe(100);
    }

    [Fact]
    public void IsAmbiguous_WhenTopCandidatesHaveNearEqualScores_ReturnsTrue()
    {
        var candidates = _matcher.Rank("hk", CreateMoves());

        _matcher.IsAmbiguous(candidates).ShouldBeTrue();
        candidates.Count(candidate => candidate.ThresholdPassed).ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Rank_WhenNoCandidateMeetsThreshold_MarksTopCandidateBelowThreshold()
    {
        var candidates = _matcher.Rank("zzzz", CreateMoves());

        candidates.ShouldNotBeEmpty();
        candidates[0].ThresholdPassed.ShouldBeFalse();
    }

    private static IReadOnlyList<Move> CreateMoves()
    {
        return
        [
            new Move
            {
                Id = "makoto-2hk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "2hk",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "8" }
            },
            new Move
            {
                Id = "makoto-5hk",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Normals",
                CanonicalName = "5hk",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "10" }
            },
            new Move
            {
                Id = "makoto-hayate",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Specials",
                CanonicalName = "Hayate",
                DisplayOrder = 3,
                FrameData = new MoveFrameData { Startup = "12" }
            }
        ];
    }

    private static IReadOnlyList<Move> CreateMoveSpecificColloquialMoves()
    {
        return
        [
            new Move
            {
                Id = "dudley-2hk",
                CharacterId = "dudley",
                Game = "sf3_3s",
                CharacterName = "Dudley",
                Section = "Normals",
                CanonicalName = "2hk",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "8" }
            },
            new Move
            {
                Id = "makoto-hayate",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Specials",
                CanonicalName = "Hayate",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "12" }
            }
        ];
    }

    private static IReadOnlyList<Move> CreateNormalVariantMoves()
    {
        return
        [
            new Move
            {
                Id = "ryu-jab",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Normals",
                CanonicalName = "Jab",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "3" }
            },
            new Move
            {
                Id = "ryu-far-jab",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Normals",
                CanonicalName = "Far Jab",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "4" }
            },
            new Move
            {
                Id = "ryu-jumping-jab",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Normals",
                CanonicalName = "Jumping Jab",
                DisplayOrder = 3,
                FrameData = new MoveFrameData { Startup = "5" }
            },
            new Move
            {
                Id = "ryu-jumping-fierce",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Normals",
                CanonicalName = "Jumping Fierce",
                DisplayOrder = 4,
                FrameData = new MoveFrameData { Startup = "6" }
            }
        ];
    }

    private static IReadOnlyList<Move> CreateSpecialMoves()
    {
        return
        [
            new Move
            {
                Id = "ryu-hadouken-jab",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Specials",
                CanonicalName = "Hadouken (Jab)",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "10" }
            },
            new Move
            {
                Id = "ryu-hadouken-strong",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Specials",
                CanonicalName = "Hadouken (Strong)",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "11" }
            },
            new Move
            {
                Id = "ryu-tatsu-short",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Specials",
                CanonicalName = "Tatsumaki Senpuu Kyaku (Short)",
                DisplayOrder = 3,
                FrameData = new MoveFrameData { Startup = "12" }
            },
            new Move
            {
                Id = "ryu-air-tatsu-short",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Specials",
                CanonicalName = "Air Tatsumaki Senpuu Kyaku (Short)",
                DisplayOrder = 4,
                FrameData = new MoveFrameData { Startup = "8" }
            },
            new Move
            {
                Id = "ryu-universal-overhead",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Misc",
                CanonicalName = "Universal Overhead",
                DisplayOrder = 5,
                FrameData = new MoveFrameData { Startup = "15" }
            },
            new Move
            {
                Id = "ken-shipuujinrai-kyaku",
                CharacterId = "ken",
                Game = "sf3_3s",
                CharacterName = "Ken",
                Section = "SuperArts",
                CanonicalName = "Shipuujinrai Kyaku",
                DisplayOrder = 6,
                FrameData = new MoveFrameData { Startup = "2" }
            }
        ];
    }
}
