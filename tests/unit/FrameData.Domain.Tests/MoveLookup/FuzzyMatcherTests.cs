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
    [InlineData("dive", "Alex", "(Air) Down + Fierce", "dive")]
    [InlineData("stomp", "Alex", "Air Stampede", "stomp")]
    [InlineData("rose", "Dudley", "Taunt", "rose")]
    [InlineData("dart shot", "Dudley", "Towards + Roundhouse", "dartshot")]
    [InlineData("kanipan", "Dudley", "Crouching Roundhouse", "kanipan")]
    [InlineData("crab punch", "Dudley", "Crouching Roundhouse", "crabpunch")]
    [InlineData("emperor punch", "Ken", "Crouching Strong", "emperorpunch")]
    [InlineData("emperors punch", "Ken", "Crouching Strong", "emperorspunch")]
    [InlineData("emperor's punch", "Ken", "Crouching Strong", "emperorspunch")]
    [InlineData("elbow cannon", "Necro", "Down Back + Fierce", "elbowcannon")]
    [InlineData("dash punch", "Q", "Dashing Head Attack", "dashpunch")]
    [InlineData("low dash punch", "Q", "Dashing Leg Attack", "lowdashpunch")]
    [InlineData("overhead dash punch", "Q", "Dashing Head Attack (Hold)", "overheaddashpunch")]
    [InlineData("slaps", "Q", "High Speed Barrage", "slaps")]
    [InlineData("command grab", "Q", "Capture and Deadly Blow", "commandgrab")]
    [InlineData("cmd grab", "Q", "Capture and Deadly Blow", "cmdgrab")]
    [InlineData("basketball", "Sean", "Taunt", "basketball")]
    [InlineData("launcher", "Urien", "Crouching Fierce", "launcher")]
    [InlineData("chesto", "Makoto", "Hayate", "chesto")]
    public void Rank_WhenInputUsesMoveSpecificColloquialAlias_RanksConfiguredMoveFirst(
        string input,
        string expectedCharacter,
        string expectedMove,
        string expectedAlias)
    {
        var candidates = _matcher.Rank(input, CreateMoveSpecificColloquialMoves());

        candidates[0].CanonicalName.ShouldBe(expectedMove);
        candidates[0].Move.CharacterName.ShouldBe(expectedCharacter);
        candidates[0].MatchedAlias.ShouldBe(expectedAlias);
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("red hadouken", "Akuma", "Shakunetsu Hadouken", "redhadouken")]
    [InlineData("red fireball", "Akuma", "Shakunetsu Hadouken", "redfireball")]
    [InlineData("kkz", "Akuma", "Kongou Kokuretsu Zan", "kkz")]
    [InlineData("demon", "Akuma", "Shun Goku Satsu", "demon")]
    [InlineData("demon", "Akuma Typo", "Shun Goku Sastu", "demon")]
    [InlineData("fireball", "Ryu", "Hadouken (Jab)", "fireball")]
    [InlineData("light fireball", "Ryu", "Hadouken (Jab)", "lightfireball")]
    [InlineData("dp", "Ryu", "Shoryuken (Jab)", "dp")]
    [InlineData("yagyou", "Oro", "Yagyou Dama", "yagyou")]
    [InlineData("booger", "Oro", "Yagyou Dama", "booger")]
    [InlineData("stones", "Oro", "Tengu Stones", "stones")]
    [InlineData("lov", "Remy", "Light of Virtue", "lov")]
    [InlineData("sonic boom", "Remy", "Light of Virtue", "sonicboom")]
    [InlineData("boom", "Remy", "Light of Virtue", "boom")]
    [InlineData("fireball", "Remy", "Light of Virtue", "fireball")]
    [InlineData("flash kick", "Remy", "Rising Rage Flash", "flashkick")]
    [InlineData("cbk", "Remy", "Cold Blue Kick", "cbk")]
    [InlineData("donkey kick", "Ryu", "Joudan Sokutou Geri", "donkeykick")]
    [InlineData("denjin", "Ryu", "Denjin Hadouken", "denjin")]
    [InlineData("roll", "Sean", "Sean Roll", "roll")]
    [InlineData("sphere", "Urien", "Metallic Sphere", "sphere")]
    [InlineData("fireball", "Urien", "Metallic Sphere", "fireball")]
    [InlineData("knee drop", "Urien", "Violence Knee Drop", "kneedrop")]
    [InlineData("knee", "Urien", "Violence Knee Drop", "knee")]
    [InlineData("tackle", "Urien", "Chariot Rush", "tackle")]
    [InlineData("shoulder", "Urien", "Chariot Rush", "shoulder")]
    [InlineData("aegis", "Urien", "Aegis Reflector", "aegis")]
    [InlineData("mirror", "Urien", "Aegis Reflector", "mirror")]
    [InlineData("slashes", "Yang", "Tourou Zan", "slashes")]
    [InlineData("rekkas", "Yang", "Tourou Zan", "rekkas")]
    [InlineData("Mantis Slash", "Yang", "Tourou Zan", "mantisslash")]
    [InlineData("zenpo", "Yang", "Zenpou Tenshin", "zenpo")]
    [InlineData("command grab", "Yang", "Zenpou Tenshin", "commandgrab")]
    [InlineData("cmd grab", "Yang", "Zenpou Tenshin", "cmdgrab")]
    [InlineData("shoulder", "Yun", "Tetsu Zankou", "shoulder")]
    [InlineData("lunch punch", "Yun", "Zesshou Hohou", "lunchpunch")]
    [InlineData("zenpo", "Yun", "Zenpou Tenshin", "zenpo")]
    [InlineData("command grab", "Yun", "Zenpou Tenshin", "commandgrab")]
    [InlineData("cmd grab", "Yun", "Zenpou Tenshin", "cmdgrab")]
    [InlineData("geneijin", "Yun", "Genei Jin", "geneijin")]
    [InlineData("backbreaker", "Hugo", "Shootdown Backbreaker", "backbreaker")]
    [InlineData("bb", "Hugo", "Shootdown Backbreaker", "bb")]
    [InlineData("spd", "Hugo", "Moonsault Press", "spd")]
    [InlineData("clap", "Hugo", "Giant Palm Bomber", "clap")]
    [InlineData("lariat", "Hugo", "Monster Lariat", "lariat")]
    [InlineData("coathanger", "Hugo", "Monster Lariat", "coathanger")]
    [InlineData("720", "Hugo", "Gigas Breaker", "720")]
    [InlineData("hammer mountain", "Hugo", "Hammer Frenzy", "hammermountain")]
    [InlineData("light dp", "Ryu", "Shoryuken (Jab)", "lightdp")]
    [InlineData("fireball", "Chun-Li", "Kikouken (Jab)", "fireball")]
    [InlineData("hadouken", "Chun-Li", "Kikouken (Jab)", "hadouken")]
    [InlineData("lightning legs", "Chun-Li", "Hyakuretsu Kyaku", "lightninglegs")]
    [InlineData("dp", "Dudley", "Jet Uppercut (Jab)", "dp")]
    [InlineData("mgb", "Dudley", "Machine Gun Blow", "mgb")]
    [InlineData("ssb", "Dudley", "Short Swing Blow", "ssb")]
    public void Rank_WhenInputUsesKnownMoveShortNameAlias_RanksExpectedMoveFirst(
        string input,
        string character,
        string expectedMove,
        string expectedAlias)
    {
        var characterMoves = CreateKnownShortNameAliasMoves()
            .Where(move => string.Equals(move.CharacterName, character, StringComparison.Ordinal));

        var candidates = _matcher.Rank(input, characterMoves);

        candidates[0].CanonicalName.ShouldBe(expectedMove);
        candidates[0].MatchedAlias.ShouldBe(expectedAlias);
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Kikoken (Jab)")]
    [InlineData("Kikouken (Jab)")]
    [InlineData("Kikkoken (Jab)")]
    public void Rank_WhenChunLiFireballUsesKnownSourceSpellings_GeneratesHadoukenAlias(string canonicalName)
    {
        var moves = new[]
        {
            new Move
            {
                Id = $"chun-li-{canonicalName}",
                CharacterId = "chun-li",
                Game = "sf3_3s",
                CharacterName = "Chun-Li",
                Section = "Specials",
                CanonicalName = canonicalName,
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "13" }
            }
        };

        var candidates = _matcher.Rank("hadouken", moves);

        candidates[0].CanonicalName.ShouldBe(canonicalName);
        candidates[0].MatchedAlias.ShouldBe("hadouken");
        candidates[0].Score.ShouldBe(100);
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
                Id = "alex-air-down-fierce",
                CharacterId = "alex",
                Game = "sf3_3s",
                CharacterName = "Alex",
                Section = "Normals",
                CanonicalName = "(Air) Down + Fierce",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "9" }
            },
            new Move
            {
                Id = "alex-air-stampede",
                CharacterId = "alex",
                Game = "sf3_3s",
                CharacterName = "Alex",
                Section = "Specials",
                CanonicalName = "Air Stampede",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "19" }
            },
            new Move
            {
                Id = "dudley-2hk",
                CharacterId = "dudley",
                Game = "sf3_3s",
                CharacterName = "Dudley",
                Section = "Normals",
                CanonicalName = "Crouching Roundhouse",
                DisplayOrder = 3,
                FrameData = new MoveFrameData { Startup = "8" }
            },
            new Move
            {
                Id = "dudley-taunt",
                CharacterId = "dudley",
                Game = "sf3_3s",
                CharacterName = "Dudley",
                Section = "Misc",
                CanonicalName = "Taunt",
                DisplayOrder = 4,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "dudley-towards-roundhouse",
                CharacterId = "dudley",
                Game = "sf3_3s",
                CharacterName = "Dudley",
                Section = "Normals",
                CanonicalName = "Towards + Roundhouse",
                DisplayOrder = 5,
                FrameData = new MoveFrameData { Startup = "17" }
            },
            new Move
            {
                Id = "makoto-hayate",
                CharacterId = "makoto",
                Game = "sf3_3s",
                CharacterName = "Makoto",
                Section = "Specials",
                CanonicalName = "Hayate",
                DisplayOrder = 6,
                FrameData = new MoveFrameData { Startup = "12" }
            },
            new Move
            {
                Id = "ken-crouching-strong",
                CharacterId = "ken",
                Game = "sf3_3s",
                CharacterName = "Ken",
                Section = "Normals",
                CanonicalName = "Crouching Strong",
                DisplayOrder = 7,
                FrameData = new MoveFrameData { Startup = "5" }
            },
            new Move
            {
                Id = "necro-down-back-fierce",
                CharacterId = "necro",
                Game = "sf3_3s",
                CharacterName = "Necro",
                Section = "Normals",
                CanonicalName = "Down Back + Fierce",
                DisplayOrder = 8,
                FrameData = new MoveFrameData { Startup = "12" }
            },
            new Move
            {
                Id = "q-dashing-head-attack",
                CharacterId = "q",
                Game = "sf3_3s",
                CharacterName = "Q",
                Section = "Specials",
                CanonicalName = "Dashing Head Attack",
                DisplayOrder = 9,
                FrameData = new MoveFrameData { Startup = "12" }
            },
            new Move
            {
                Id = "q-dashing-head-attack-hold",
                CharacterId = "q",
                Game = "sf3_3s",
                CharacterName = "Q",
                Section = "Specials",
                CanonicalName = "Dashing Head Attack (Hold)",
                DisplayOrder = 10,
                FrameData = new MoveFrameData { Startup = "25" }
            },
            new Move
            {
                Id = "q-dashing-leg-attack",
                CharacterId = "q",
                Game = "sf3_3s",
                CharacterName = "Q",
                Section = "Specials",
                CanonicalName = "Dashing Leg Attack",
                DisplayOrder = 11,
                FrameData = new MoveFrameData { Startup = "14" }
            },
            new Move
            {
                Id = "q-high-speed-barrage",
                CharacterId = "q",
                Game = "sf3_3s",
                CharacterName = "Q",
                Section = "SuperArts",
                CanonicalName = "High Speed Barrage",
                DisplayOrder = 12,
                FrameData = new MoveFrameData { Startup = "2" }
            },
            new Move
            {
                Id = "q-capture-and-deadly-blow",
                CharacterId = "q",
                Game = "sf3_3s",
                CharacterName = "Q",
                Section = "Specials",
                CanonicalName = "Capture and Deadly Blow",
                DisplayOrder = 13,
                FrameData = new MoveFrameData { Startup = "7" }
            },
            new Move
            {
                Id = "sean-taunt",
                CharacterId = "sean",
                Game = "sf3_3s",
                CharacterName = "Sean",
                Section = "Misc",
                CanonicalName = "Taunt",
                DisplayOrder = 14,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "urien-crouching-fierce",
                CharacterId = "urien",
                Game = "sf3_3s",
                CharacterName = "Urien",
                Section = "Normals",
                CanonicalName = "Crouching Fierce",
                DisplayOrder = 15,
                FrameData = new MoveFrameData { Startup = "8" }
            }
        ];
    }

    private static IReadOnlyList<Move> CreateKnownShortNameAliasMoves()
    {
        return
        [
            new Move
            {
                Id = "akuma-shakunetsu-hadouken",
                CharacterId = "akuma",
                Game = "sf3_3s",
                CharacterName = "Akuma",
                Section = "Specials",
                CanonicalName = "Shakunetsu Hadouken",
                DisplayOrder = 1,
                FrameData = new MoveFrameData { Startup = "12" }
            },
            new Move
            {
                Id = "akuma-kongou-kokuretsu-zan",
                CharacterId = "akuma",
                Game = "sf3_3s",
                CharacterName = "Akuma",
                Section = "SuperArts",
                CanonicalName = "Kongou Kokuretsu Zan",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "akuma-shun-goku-satsu",
                CharacterId = "akuma",
                Game = "sf3_3s",
                CharacterName = "Akuma",
                Section = "SuperArts",
                CanonicalName = "Shun Goku Satsu",
                DisplayOrder = 3,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "akuma-shun-goku-sastu",
                CharacterId = "akuma",
                Game = "sf3_3s",
                CharacterName = "Akuma Typo",
                Section = "SuperArts",
                CanonicalName = "Shun Goku Sastu",
                DisplayOrder = 4,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "hugo-shootdown-backbreaker",
                CharacterId = "hugo",
                Game = "sf3_3s",
                CharacterName = "Hugo",
                Section = "Specials",
                CanonicalName = "Shootdown Backbreaker",
                DisplayOrder = 5,
                FrameData = new MoveFrameData { Startup = "5" }
            },
            new Move
            {
                Id = "hugo-moonsault-press",
                CharacterId = "hugo",
                Game = "sf3_3s",
                CharacterName = "Hugo",
                Section = "Specials",
                CanonicalName = "Moonsault Press",
                DisplayOrder = 6,
                FrameData = new MoveFrameData { Startup = "2" }
            },
            new Move
            {
                Id = "hugo-giant-palm-bomber",
                CharacterId = "hugo",
                Game = "sf3_3s",
                CharacterName = "Hugo",
                Section = "Specials",
                CanonicalName = "Giant Palm Bomber",
                DisplayOrder = 7,
                FrameData = new MoveFrameData { Startup = "11" }
            },
            new Move
            {
                Id = "hugo-monster-lariat",
                CharacterId = "hugo",
                Game = "sf3_3s",
                CharacterName = "Hugo",
                Section = "Specials",
                CanonicalName = "Monster Lariat",
                DisplayOrder = 8,
                FrameData = new MoveFrameData { Startup = "16" }
            },
            new Move
            {
                Id = "hugo-gigas-breaker",
                CharacterId = "hugo",
                Game = "sf3_3s",
                CharacterName = "Hugo",
                Section = "SuperArts",
                CanonicalName = "Gigas Breaker",
                DisplayOrder = 9,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "hugo-hammer-frenzy",
                CharacterId = "hugo",
                Game = "sf3_3s",
                CharacterName = "Hugo",
                Section = "SuperArts",
                CanonicalName = "Hammer Frenzy",
                DisplayOrder = 10,
                FrameData = new MoveFrameData { Startup = "1" }
            },
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
                Id = "oro-yagyou-dama",
                CharacterId = "oro",
                Game = "sf3_3s",
                CharacterName = "Oro",
                Section = "SuperArts",
                CanonicalName = "Yagyou Dama",
                DisplayOrder = 11,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "oro-tengu-stones",
                CharacterId = "oro",
                Game = "sf3_3s",
                CharacterName = "Oro",
                Section = "SuperArts",
                CanonicalName = "Tengu Stones",
                DisplayOrder = 12,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "ryu-shoryuken-jab",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Specials",
                CanonicalName = "Shoryuken (Jab)",
                DisplayOrder = 2,
                FrameData = new MoveFrameData { Startup = "3" }
            },
            new Move
            {
                Id = "remy-light-of-virtue",
                CharacterId = "remy",
                Game = "sf3_3s",
                CharacterName = "Remy",
                Section = "Specials",
                CanonicalName = "Light of Virtue",
                DisplayOrder = 13,
                FrameData = new MoveFrameData { Startup = "11" }
            },
            new Move
            {
                Id = "remy-rising-rage-flash",
                CharacterId = "remy",
                Game = "sf3_3s",
                CharacterName = "Remy",
                Section = "Specials",
                CanonicalName = "Rising Rage Flash",
                DisplayOrder = 14,
                FrameData = new MoveFrameData { Startup = "5" }
            },
            new Move
            {
                Id = "remy-cold-blue-kick",
                CharacterId = "remy",
                Game = "sf3_3s",
                CharacterName = "Remy",
                Section = "Specials",
                CanonicalName = "Cold Blue Kick",
                DisplayOrder = 15,
                FrameData = new MoveFrameData { Startup = "19" }
            },
            new Move
            {
                Id = "ryu-joudan-sokutou-geri",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "Specials",
                CanonicalName = "Joudan Sokutou Geri",
                DisplayOrder = 16,
                FrameData = new MoveFrameData { Startup = "14" }
            },
            new Move
            {
                Id = "ryu-denjin-hadouken",
                CharacterId = "ryu",
                Game = "sf3_3s",
                CharacterName = "Ryu",
                Section = "SuperArts",
                CanonicalName = "Denjin Hadouken",
                DisplayOrder = 17,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "sean-roll",
                CharacterId = "sean",
                Game = "sf3_3s",
                CharacterName = "Sean",
                Section = "Specials",
                CanonicalName = "Sean Roll",
                DisplayOrder = 18,
                FrameData = new MoveFrameData { Startup = "20" }
            },
            new Move
            {
                Id = "urien-metallic-sphere",
                CharacterId = "urien",
                Game = "sf3_3s",
                CharacterName = "Urien",
                Section = "Specials",
                CanonicalName = "Metallic Sphere",
                DisplayOrder = 19,
                FrameData = new MoveFrameData { Startup = "14" }
            },
            new Move
            {
                Id = "urien-violence-knee-drop",
                CharacterId = "urien",
                Game = "sf3_3s",
                CharacterName = "Urien",
                Section = "Specials",
                CanonicalName = "Violence Knee Drop",
                DisplayOrder = 20,
                FrameData = new MoveFrameData { Startup = "13" }
            },
            new Move
            {
                Id = "urien-chariot-rush",
                CharacterId = "urien",
                Game = "sf3_3s",
                CharacterName = "Urien",
                Section = "Specials",
                CanonicalName = "Chariot Rush",
                DisplayOrder = 21,
                FrameData = new MoveFrameData { Startup = "10" }
            },
            new Move
            {
                Id = "urien-aegis-reflector",
                CharacterId = "urien",
                Game = "sf3_3s",
                CharacterName = "Urien",
                Section = "SuperArts",
                CanonicalName = "Aegis Reflector",
                DisplayOrder = 22,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "yang-tourou-zan",
                CharacterId = "yang",
                Game = "sf3_3s",
                CharacterName = "Yang",
                Section = "Specials",
                CanonicalName = "Tourou Zan",
                DisplayOrder = 23,
                FrameData = new MoveFrameData { Startup = "12" }
            },
            new Move
            {
                Id = "yang-zenpou-tenshin",
                CharacterId = "yang",
                Game = "sf3_3s",
                CharacterName = "Yang",
                Section = "Specials",
                CanonicalName = "Zenpou Tenshin",
                DisplayOrder = 24,
                FrameData = new MoveFrameData { Startup = "7" }
            },
            new Move
            {
                Id = "yun-tetsu-zankou",
                CharacterId = "yun",
                Game = "sf3_3s",
                CharacterName = "Yun",
                Section = "Specials",
                CanonicalName = "Tetsu Zankou",
                DisplayOrder = 25,
                FrameData = new MoveFrameData { Startup = "11" }
            },
            new Move
            {
                Id = "yun-zesshou-hohou",
                CharacterId = "yun",
                Game = "sf3_3s",
                CharacterName = "Yun",
                Section = "Specials",
                CanonicalName = "Zesshou Hohou",
                DisplayOrder = 26,
                FrameData = new MoveFrameData { Startup = "13" }
            },
            new Move
            {
                Id = "yun-zenpou-tenshin",
                CharacterId = "yun",
                Game = "sf3_3s",
                CharacterName = "Yun",
                Section = "Specials",
                CanonicalName = "Zenpou Tenshin",
                DisplayOrder = 27,
                FrameData = new MoveFrameData { Startup = "7" }
            },
            new Move
            {
                Id = "yun-genei-jin",
                CharacterId = "yun",
                Game = "sf3_3s",
                CharacterName = "Yun",
                Section = "SuperArts",
                CanonicalName = "Genei Jin",
                DisplayOrder = 28,
                FrameData = new MoveFrameData { Startup = "1" }
            },
            new Move
            {
                Id = "chun-li-kikouken-jab",
                CharacterId = "chun-li",
                Game = "sf3_3s",
                CharacterName = "Chun-Li",
                Section = "Specials",
                CanonicalName = "Kikouken (Jab)",
                DisplayOrder = 3,
                FrameData = new MoveFrameData { Startup = "13" }
            },
            new Move
            {
                Id = "chun-li-hyakuretsu-kyaku",
                CharacterId = "chun-li",
                Game = "sf3_3s",
                CharacterName = "Chun-Li",
                Section = "Specials",
                CanonicalName = "Hyakuretsu Kyaku",
                DisplayOrder = 4,
                FrameData = new MoveFrameData { Startup = "4" }
            },
            new Move
            {
                Id = "dudley-jet-uppercut-jab",
                CharacterId = "dudley",
                Game = "sf3_3s",
                CharacterName = "Dudley",
                Section = "Specials",
                CanonicalName = "Jet Uppercut (Jab)",
                DisplayOrder = 5,
                FrameData = new MoveFrameData { Startup = "4" }
            },
            new Move
            {
                Id = "dudley-machine-gun-blow",
                CharacterId = "dudley",
                Game = "sf3_3s",
                CharacterName = "Dudley",
                Section = "Specials",
                CanonicalName = "Machine Gun Blow",
                DisplayOrder = 6,
                FrameData = new MoveFrameData { Startup = "11" }
            },
            new Move
            {
                Id = "dudley-short-swing-blow",
                CharacterId = "dudley",
                Game = "sf3_3s",
                CharacterName = "Dudley",
                Section = "Specials",
                CanonicalName = "Short Swing Blow",
                DisplayOrder = 7,
                FrameData = new MoveFrameData { Startup = "7" }
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
