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
}
