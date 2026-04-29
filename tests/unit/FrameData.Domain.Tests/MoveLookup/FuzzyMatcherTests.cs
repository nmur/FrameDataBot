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

    [Theory]
    [InlineData("sa1", "First Super", "sa1")]
    [InlineData("super art 2", "Second Super", "superart2")]
    [InlineData("sa3", "Third Super", "sa3")]
    public void Rank_WhenInputUsesGenericSuperArtAlias_UsesSuperArtDisplayOrder(
        string input,
        string expectedMove,
        string expectedAlias)
    {
        var candidates = _matcher.Rank(input, CreateGenericSuperArtMoves());

        candidates[0].CanonicalName.ShouldBe(expectedMove);
        candidates[0].MatchedAlias.ShouldBe(expectedAlias);
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Theory]
    [InlineData("sa1", "Alex", "Hyper Bomb", "sa1")]
    [InlineData("reverse sa1", "Alex", "Reverse Hyper Bomb", "reversesa1")]
    [InlineData("back sa1", "Alex", "Reverse Hyper Bomb", "5sa1")]
    [InlineData("from behind sa1", "Alex", "Reverse Hyper Bomb", "frombehindsa1")]
    [InlineData("sa2", "Alex", "Boomerang Raid", "sa2")]
    [InlineData("sa3", "Alex", "Stun Gun Headbutt", "sa3")]
    [InlineData("sa1", "Akuma", "Messatsu Gou Hadou", "sa1")]
    [InlineData("air sa1", "Akuma", "Tenma Gou Zankuu", "jsa1")]
    [InlineData("jp sa1", "Akuma", "Tenma Gou Zankuu", "jsa1")]
    [InlineData("jumping sa1", "Akuma", "Tenma Gou Zankuu", "jsa1")]
    [InlineData("sa2", "Akuma", "Messatsu Gou Shoryuu", "sa2")]
    [InlineData("sa3", "Akuma", "Messatsu Gou Rasen (Ground)", "sa3")]
    [InlineData("air sa3", "Akuma", "Messatsu Gou Rasen (Air)", "jsa3")]
    [InlineData("light sa2", "Hugo", "Megaton Press (Jab)", "lightsa2")]
    [InlineData("sa2 mp", "Hugo", "Megaton Press (Strong)", "sa2mp")]
    [InlineData("hp sa2", "Hugo", "Megaton Press (Fierce)", "hpsa2")]
    [InlineData("sa2", "Ibuki", "Yoroi Doushi", "sa2")]
    [InlineData("missed sa2", "Ibuki", "Missed grab (Chi Blast)", "missedsa2")]
    [InlineData("whiffed sa2", "Ibuki", "Missed grab (Chi Blast)", "whiffedsa2")]
    [InlineData("sa1", "Oro", "Kishin Riki", "sa1")]
    [InlineData("sa1 grab", "Oro", "Ground Grab", "sa1grab")]
    [InlineData("sa1 throw", "Oro", "Ground Grab", "sa1throw")]
    [InlineData("sa1 air grab", "Oro", "Air Grab", "sa1airgrab")]
    [InlineData("sa1 air throw", "Oro", "Air Grab", "sa1airthrow")]
    [InlineData("ex sa1", "Oro", "EX Kishin Riki", "exsa1")]
    [InlineData("sa2", "Oro", "Yagyou Dama", "sa2")]
    [InlineData("ex sa2", "Oro", "EX Yagyou Dama", "exsa2")]
    [InlineData("sa3", "Oro", "Tengu Stones", "sa3")]
    [InlineData("sa3", "Q", "Total Destruction", "sa3")]
    [InlineData("sa3 punch", "Q", "Far Grab", "sa3punch")]
    [InlineData("sa3 kick", "Q", "Close Grab", "sa3kick")]
    [InlineData("sa1", "Ryu", "Shinku Hadouken", "sa1")]
    [InlineData("sa2", "Ryu", "Shin Shoryuken", "sa2")]
    [InlineData("far sa2", "Ryu", "Shin Shoryuken (Far)", "farsa2")]
    [InlineData("missed sa2", "Ryu", "Shin Shoryuken (Far)", "missedsa2")]
    [InlineData("whiffed sa2", "Ryu", "Shin Shoryuken (Far)", "whiffedsa2")]
    [InlineData("sa3", "Ryu", "Denjin Hadouken", "sa3")]
    [InlineData("sa2", "Twelve", "X.F.L.A.T.", "sa2")]
    [InlineData("air sa2", "Twelve", "X.F.L.A.T. (Air hit)", "jsa2")]
    [InlineData("sa3", "Twelve", "X.C.O.P.Y.", "sa3")]
    [InlineData("sa3", "Urien", "Aegis Reflector", "sa3")]
    [InlineData("ex sa3", "Urien", "Aegis Reflector (EX)", "exsa3")]
    public void Rank_WhenInputUsesSuperArtAliasException_RanksConfiguredMoveFirst(
        string input,
        string character,
        string expectedMove,
        string expectedAlias)
    {
        var characterMoves = CreateSuperArtExceptionMoves()
            .Where(move => string.Equals(move.CharacterName, character, StringComparison.Ordinal));

        var candidates = _matcher.Rank(input, characterMoves);

        candidates[0].CanonicalName.ShouldBe(expectedMove);
        candidates[0].MatchedAlias.ShouldBe(expectedAlias);
        candidates[0].Score.ShouldBe(100);
        candidates[0].ThresholdPassed.ShouldBeTrue();
    }

    [Fact]
    public void Rank_WhenTwelveHasTwoXCopyRows_Sa3TargetsFirstXCopy()
    {
        var characterMoves = CreateSuperArtExceptionMoves()
            .Where(move => string.Equals(move.CharacterName, "Twelve", StringComparison.Ordinal));

        var candidates = _matcher.Rank("sa3", characterMoves);

        candidates[0].MoveId.ShouldBe("twelve-xcopy-first");
        candidates[0].CanonicalName.ShouldBe("X.C.O.P.Y.");
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
    [InlineData("air knee smash rh", "Air Knee Smash (RH)", "jkneesmashhk")]
    [InlineData("air knee smash roundhouse", "Air Knee Smash (RH)", "jkneesmashhk")]
    [InlineData("air knee smash heavy kick", "Air Knee Smash (RH)", "jkneesmashhk")]
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

    [Fact]
    public void Rank_WhenInputUsesExShortName_RanksExVariantWithoutAmbiguity()
    {
        var candidates = _matcher.Rank("ex mgb", CreateSpecialMoveStrengthVariants());

        candidates[0].CanonicalName.ShouldBe("Machine Gun Blow (EX)");
        candidates[0].MatchedAlias.ShouldBe("exmgb");
        candidates[0].Score.ShouldBe(100);
        _matcher.IsAmbiguous(candidates).ShouldBeFalse();
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

    private static IReadOnlyList<Move> CreateGenericSuperArtMoves()
    {
        return
        [
            CreateTestMove("generic-5lp", "generic", "Generic", "Normals", "5lp", 1),
            CreateTestMove("generic-first-super", "generic", "Generic", "SuperArts", "First Super", 10),
            CreateTestMove("generic-second-super", "generic", "Generic", "SuperArts", "Second Super", 20),
            CreateTestMove("generic-third-super", "generic", "Generic", "SuperArts", "Third Super", 30)
        ];
    }

    private static IReadOnlyList<Move> CreateSuperArtExceptionMoves()
    {
        return
        [
            CreateTestMove("alex-hyper-bomb", "alex", "Alex", "SuperArts", "Hyper Bomb", 1),
            CreateTestMove("alex-reverse-hyper-bomb", "alex", "Alex", "SuperArts", "Reverse Hyper Bomb", 2),
            CreateTestMove("alex-boomerang-raid", "alex", "Alex", "SuperArts", "Boomerang Raid", 3),
            CreateTestMove("alex-stun-gun-headbutt", "alex", "Alex", "SuperArts", "Stun Gun Headbutt", 4),

            CreateTestMove("akuma-messatsu-gou-hadou", "akuma", "Akuma", "SuperArts", "Messatsu Gou Hadou", 1),
            CreateTestMove("akuma-tenma-gou-zankuu", "akuma", "Akuma", "SuperArts", "Tenma Gou Zankuu", 2),
            CreateTestMove("akuma-messatsu-gou-shoryuu", "akuma", "Akuma", "SuperArts", "Messatsu Gou Shoryuu", 3),
            CreateTestMove("akuma-messatsu-gou-rasen-ground", "akuma", "Akuma", "SuperArts", "Messatsu Gou Rasen (Ground)", 4),
            CreateTestMove("akuma-messatsu-gou-rasen-air", "akuma", "Akuma", "SuperArts", "Messatsu Gou Rasen (Air)", 5),
            CreateTestMove("akuma-kongou-kokuretsu-zan", "akuma", "Akuma", "SuperArts", "Kongou Kokuretsu Zan", 6),

            CreateTestMove("hugo-gigas-breaker", "hugo", "Hugo", "SuperArts", "Gigas Breaker", 1),
            CreateTestMove("hugo-megaton-press-jab", "hugo", "Hugo", "SuperArts", "Megaton Press (Jab)", 2),
            CreateTestMove("hugo-megaton-press-strong", "hugo", "Hugo", "SuperArts", "Megaton Press (Strong)", 3),
            CreateTestMove("hugo-megaton-press-fierce", "hugo", "Hugo", "SuperArts", "Megaton Press (Fierce)", 4),
            CreateTestMove("hugo-hammer-frenzy", "hugo", "Hugo", "SuperArts", "Hammer Frenzy", 5),

            CreateTestMove("ibuki-kasumi-suzaku", "ibuki", "Ibuki", "SuperArts", "Kasumi Suzaku", 1),
            CreateTestMove("ibuki-yoroi-doushi", "ibuki", "Ibuki", "SuperArts", "Yoroi Doushi", 2),
            CreateTestMove("ibuki-missed-grab-chi-blast", "ibuki", "Ibuki", "SuperArts", "Missed grab (Chi Blast)", 3),
            CreateTestMove("ibuki-yami-shigure", "ibuki", "Ibuki", "SuperArts", "Yami Shigure", 4),

            CreateTestMove("oro-kishin-riki", "oro", "Oro", "SuperArts", "Kishin Riki", 1),
            CreateTestMove("oro-ground-grab", "oro", "Oro", "SuperArts", "Ground Grab", 2),
            CreateTestMove("oro-air-grab", "oro", "Oro", "SuperArts", "Air Grab", 3),
            CreateTestMove("oro-ex-kishin-riki", "oro", "Oro", "SuperArts", "EX Kishin Riki", 4),
            CreateTestMove("oro-yagyou-dama", "oro", "Oro", "SuperArts", "Yagyou Dama", 5),
            CreateTestMove("oro-ex-yagyou-dama", "oro", "Oro", "SuperArts", "EX Yagyou Dama", 6),
            CreateTestMove("oro-tengu-stones", "oro", "Oro", "SuperArts", "Tengu Stones", 7),

            CreateTestMove("q-critical-combo-attack", "q", "Q", "SuperArts", "Critical Combo Attack", 1),
            CreateTestMove("q-deadly-double-combination", "q", "Q", "SuperArts", "Deadly Double Combination", 2),
            CreateTestMove("q-total-destruction", "q", "Q", "SuperArts", "Total Destruction", 3),
            CreateTestMove("q-far-grab", "q", "Q", "SuperArts", "Far Grab", 4),
            CreateTestMove("q-close-grab", "q", "Q", "SuperArts", "Close Grab", 5),

            CreateTestMove("ryu-shinku-hadouken", "ryu", "Ryu", "SuperArts", "Shinku Hadouken", 1),
            CreateTestMove("ryu-shin-shoryuken", "ryu", "Ryu", "SuperArts", "Shin Shoryuken", 2),
            CreateTestMove("ryu-shin-shoryuken-far", "ryu", "Ryu", "SuperArts", "Shin Shoryuken (Far)", 3),
            CreateTestMove("ryu-denjin-hadouken", "ryu", "Ryu", "SuperArts", "Denjin Hadouken", 4),

            CreateTestMove("twelve-xndl", "twelve", "Twelve", "SuperArts", "X.N.D.L.", 1),
            CreateTestMove("twelve-xflat", "twelve", "Twelve", "SuperArts", "X.F.L.A.T.", 2),
            CreateTestMove("twelve-xflat-air-hit", "twelve", "Twelve", "SuperArts", "X.F.L.A.T. (Air hit)", 3),
            CreateTestMove("twelve-xcopy-first", "twelve", "Twelve", "SuperArts", "X.C.O.P.Y.", 4),
            CreateTestMove("twelve-xcopy-second", "twelve", "Twelve", "SuperArts", "X.C.O.P.Y.", 5),

            CreateTestMove("urien-tyrant-slaughter", "urien", "Urien", "SuperArts", "Tyrant Slaughter", 1),
            CreateTestMove("urien-temporal-thunder", "urien", "Urien", "SuperArts", "Temporal Thunder", 2),
            CreateTestMove("urien-aegis-reflector", "urien", "Urien", "SuperArts", "Aegis Reflector", 3),
            CreateTestMove("urien-aegis-reflector-ex", "urien", "Urien", "SuperArts", "Aegis Reflector (EX)", 4)
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

    private static Move CreateTestMove(
        string id,
        string characterId,
        string characterName,
        string section,
        string canonicalName,
        int displayOrder)
        => new()
        {
            Id = id,
            CharacterId = characterId,
            Game = "sf3_3s",
            CharacterName = characterName,
            Section = section,
            CanonicalName = canonicalName,
            DisplayOrder = displayOrder,
            FrameData = new MoveFrameData { Startup = "1" }
        };

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

    private static IReadOnlyList<Move> CreateSpecialMoveStrengthVariants()
    {
        return
        [
            CreateTestMove("dudley-machine-gun-blow-jab", "dudley", "Dudley", "Specials", "Machine Gun Blow (Jab)", 1),
            CreateTestMove("dudley-machine-gun-blow-strong", "dudley", "Dudley", "Specials", "Machine Gun Blow (Strong)", 2),
            CreateTestMove("dudley-machine-gun-blow-fierce", "dudley", "Dudley", "Specials", "Machine Gun Blow (Fierce)", 3),
            CreateTestMove("dudley-machine-gun-blow-ex", "dudley", "Dudley", "Specials", "Machine Gun Blow (EX)", 4)
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
            },
            new Move
            {
                Id = "alex-air-knee-smash-rh",
                CharacterId = "alex",
                Game = "sf3_3s",
                CharacterName = "Alex",
                Section = "Specials",
                CanonicalName = "Air Knee Smash (RH)",
                DisplayOrder = 7,
                FrameData = new MoveFrameData { Startup = "5" }
            }
        ];
    }
}
