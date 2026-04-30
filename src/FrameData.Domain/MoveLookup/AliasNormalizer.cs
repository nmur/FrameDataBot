using System.Text.RegularExpressions;
using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public sealed partial class AliasNormalizer
{
    private static readonly IReadOnlyDictionary<string, string[]> MoveSpecificColloquialAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["alex:jdownhp"] = ["dive"],
        ["alex:jstampede"] = ["stomp"],
        ["dudley:2hk"] = ["kanipan", "crab punch"],
        ["dudley:taunt"] = ["rose"],
        ["dudley:6hk"] = ["dart shot"],
        ["dudley:towardhk"] = ["dart shot"],
        ["dudley:towardshk"] = ["dart shot"],
        ["ken:2mp"] = ["emperor punch", "emperors punch", "emperor's punch"],
        ["necro:1hp"] = ["elbow cannon"],
        ["q:captureanddeadlyblow"] = ["command grab", "cmd grab"],
        ["q:dashingheadattack"] = ["dash punch"],
        ["q:dashingheadattackhold"] = ["overhead dash punch"],
        ["q:dashinglegattack"] = ["low dash punch"],
        ["q:highspeedbarrage"] = ["slaps"],
        ["sean:taunt"] = ["basketball"],
        ["urien:2hp"] = ["launcher"],
        ["makoto:hayate"] = ["chesto"]
    };

    private static readonly IReadOnlyDictionary<string, string[]> KnownMoveShortNameAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["gigasbreaker"] = ["720"],
        ["giantpalmbomber"] = ["clap"],
        ["geneijin"] = ["geneijin"],
        ["hadouken"] = ["fireball"],
        ["hammerfrenzy"] = ["hammer mountain"],
        ["hyakuretsukyaku"] = ["lightning legs"],
        ["jetuppercut"] = ["dp"],
        ["kikoken"] = ["fireball", "hadouken"],
        ["kikouken"] = ["fireball", "hadouken"],
        ["kikkoken"] = ["fireball", "hadouken"],
        ["kongoukokuretsuzan"] = ["kkz"],
        ["coldbluekick"] = ["cbk"],
        ["denjinhadouken"] = ["denjin"],
        ["joudansokutougeri"] = ["donkey kick"],
        ["lightofvirtue"] = ["lov", "sonic boom", "boom", "fireball"],
        ["lkswingblow"] = ["ssb"],
        ["machinegunblow"] = ["mgb"],
        ["monsterlariat"] = ["lariat", "coathanger"],
        ["moonsaultpress"] = ["spd"],
        ["risingrageflash"] = ["flash kick"],
        ["aegisreflector"] = ["aegis", "mirror"],
        ["chariotrush"] = ["tackle", "shoulder"],
        ["metallicsphere"] = ["sphere", "fireball"],
        ["seanroll"] = ["roll"],
        ["shakunetsuhadouken"] = ["red hadouken", "red fireball"],
        ["shoryuken"] = ["dp"],
        ["shootdownbackbreaker"] = ["backbreaker", "bb"],
        ["shortswingblow"] = ["ssb"],
        ["shungokusatsu"] = ["demon"],
        ["shungokusastu"] = ["demon"],
        ["tatsumakisenpuukyaku"] = ["tatsu"],
        ["tetsuzankou"] = ["shoulder"],
        ["tourouzan"] = ["slashes", "rekkas", "mantis slash"],
        ["violencekneedrop"] = ["knee drop", "knee"],
        ["shipuujinraikyaku"] = ["shipu", "shippu"],
        ["tengustones"] = ["stones"],
        ["universaloverhead"] = ["uoh"],
        ["yagyoudama"] = ["yagyou", "booger"],
        ["zenpoutenshin"] = ["zenpo", "command grab", "cmd grab"],
        ["zesshouhohou"] = ["lunch punch"]
    };

    private static readonly IReadOnlyDictionary<string, StrengthAlias> ParentheticalStrengthAliases = new Dictionary<string, StrengthAlias>(StringComparer.OrdinalIgnoreCase)
    {
        ["jab"] = new StrengthAlias("light", "lp"),
        ["strong"] = new StrengthAlias("medium", "mp"),
        ["fierce"] = new StrengthAlias("heavy", "hp"),
        ["short"] = new StrengthAlias("light", "lk"),
        ["forward"] = new StrengthAlias("medium", "mk"),
        ["roundhouse"] = new StrengthAlias("heavy", "hk"),
        ["lp"] = new StrengthAlias("light", "lp"),
        ["mp"] = new StrengthAlias("medium", "mp"),
        ["hp"] = new StrengthAlias("heavy", "hp"),
        ["lk"] = new StrengthAlias("light", "lk"),
        ["mk"] = new StrengthAlias("medium", "mk"),
        ["hk"] = new StrengthAlias("heavy", "hk"),
        ["rh"] = new StrengthAlias("heavy", "hk"),
        ["ex"] = new StrengthAlias("ex", "ex")
    };

    private static readonly IReadOnlyDictionary<string, string> AttackTermAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["light punch"] = "lp",
        ["jab"] = "lp",
        ["medium punch"] = "mp",
        ["strong"] = "mp",
        ["heavy punch"] = "hp",
        ["fierce"] = "hp",
        ["light kick"] = "lk",
        ["short"] = "lk",
        ["medium kick"] = "mk",
        ["forward"] = "mk",
        ["heavy kick"] = "hk",
        ["high kick"] = "hk",
        ["roundhouse"] = "hk",
        ["rh"] = "hk",
        ["low forward"] = "2mk"
    };

    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var value = input.Trim().ToLowerInvariant();
        value = ApplyMotionPhraseAliases(value);
        value = ApplyDirectionalPhraseAliases(value);
        foreach (var alias in AttackTermAliases.OrderByDescending(alias => alias.Key.Length))
        {
            value = Regex.Replace(value, $@"\b{Regex.Escape(alias.Key)}\b", alias.Value, RegexOptions.IgnoreCase);
        }

        var compact = NonLookupCharacters().Replace(value, string.Empty);
        return ApplyPositionPrefix(compact);
    }

    public IReadOnlyList<MoveAlias> CreateAliases(Move move, IReadOnlyList<Move>? characterMoves = null)
    {
        var aliases = new List<MoveAlias>();
        AddAlias(aliases, move, move.CanonicalName, MoveAliasType.Canonical);

        var normalizedCanonical = Normalize(move.CanonicalName);
        AddDerivedNotationAliases(aliases, move, normalizedCanonical);
        AddCloseNormalAliases(aliases, move, normalizedCanonical);
        AddSpecialMoveStrengthAliases(aliases, move);
        AddMotionAliases(aliases, move);
        AddKnownMoveShortNameAliases(aliases, move);
        AddMoveSpecificColloquialAliases(aliases, move, normalizedCanonical);
        AddColloquialAliases(aliases, move, normalizedCanonical);
        AddSuperArtAliases(aliases, move, normalizedCanonical, characterMoves);

        return aliases
            .GroupBy(alias => alias.NormalizedAlias, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(alias => alias.AliasType is MoveAliasType.Canonical ? 1 : 0)
                .ThenBy(alias => alias.AliasType)
                .First())
            .ToArray();
    }

    private void AddDerivedNotationAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        if (normalizedCanonical.Length < 3)
        {
            return;
        }

        var button = normalizedCanonical[1..];
        switch (normalizedCanonical[0])
        {
            case '2':
                AddAlias(aliases, move, $"cr.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"c.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"crouching {button}", MoveAliasType.Numpad);
                break;
            case '5':
                AddAlias(aliases, move, $"st.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"s.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"standing {button}", MoveAliasType.Numpad);
                break;
            case '1':
                AddAlias(aliases, move, $"db.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"d/b.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"down back {button}", MoveAliasType.Numpad);
                AddAlias(aliases, move, $"down-back {button}", MoveAliasType.Numpad);
                break;
            case '3':
                AddAlias(aliases, move, $"df.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"d/f.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"down forward {button}", MoveAliasType.Numpad);
                AddAlias(aliases, move, $"down-forward {button}", MoveAliasType.Numpad);
                break;
            case '6':
                AddAlias(aliases, move, $"toward {button}", MoveAliasType.Numpad);
                AddAlias(aliases, move, $"towards {button}", MoveAliasType.Numpad);
                break;
            case 'j':
                AddAlias(aliases, move, $"j.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"jp.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"jumping {button}", MoveAliasType.Numpad);
                AddAlias(aliases, move, $"jump {button}", MoveAliasType.Numpad);
                AddAlias(aliases, move, $"air {button}", MoveAliasType.Numpad);
                break;
        }
    }

    private void AddCloseNormalAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        var button = normalizedCanonical.Length switch
        {
            2 when IsButtonSuffix(normalizedCanonical) => normalizedCanonical,
            3 when normalizedCanonical.StartsWith('5') && IsButtonSuffix(normalizedCanonical[1..]) => normalizedCanonical[1..],
            _ => null
        };

        if (button is null)
        {
            return;
        }

        AddAlias(aliases, move, $"close {button}", MoveAliasType.Derived);
        AddAlias(aliases, move, $"cl {button}", MoveAliasType.Abbreviation);
    }

    private void AddSpecialMoveStrengthAliases(List<MoveAlias> aliases, Move move)
    {
        var match = ParentheticalVariantName().Match(move.CanonicalName);
        if (!match.Success)
        {
            return;
        }

        var baseName = match.Groups["baseName"].Value.Trim();
        var indicator = Normalize(match.Groups["indicator"].Value);
        if (!ParentheticalStrengthAliases.TryGetValue(indicator, out var strengthAlias))
        {
            return;
        }

        AddAlias(aliases, move, $"{strengthAlias.Strength} {baseName}", MoveAliasType.Derived);
        AddAlias(aliases, move, $"{baseName} {strengthAlias.Strength}", MoveAliasType.Derived);
        AddAlias(aliases, move, $"{strengthAlias.Button} {baseName}", MoveAliasType.Abbreviation);
        AddAlias(aliases, move, $"{baseName} {strengthAlias.Button}", MoveAliasType.Abbreviation);
    }

    private void AddMotionAliases(List<MoveAlias> aliases, Move move)
    {
        if (string.IsNullOrWhiteSpace(move.Motion))
        {
            return;
        }

        var normalizedMotion = Normalize(move.Motion);
        AddAlias(aliases, move, move.Motion, MoveAliasType.Numpad);

        var motionParts = TryGetMotionParts(normalizedMotion);
        if (motionParts is null)
        {
            return;
        }

        AddAlias(aliases, move, motionParts.Value.Motion, MoveAliasType.Numpad);

        var parentheticalStrength = GetParentheticalStrengthAlias(move.CanonicalName);
        var buttons = ExtractMotionButtons(motionParts.Value.Suffix);
        foreach (var button in SelectMotionButtonAliases(buttons, parentheticalStrength))
        {
            AddAlias(aliases, move, $"{motionParts.Value.Motion}{button}", MoveAliasType.Numpad);
        }
    }

    private void AddKnownMoveShortNameAliases(List<MoveAlias> aliases, Move move)
    {
        var baseName = GetMoveBaseName(move.CanonicalName);
        var movement = StripMovementPrefix(baseName);
        var normalizedBase = Normalize(movement.BaseName);

        if (!KnownMoveShortNameAliases.TryGetValue(normalizedBase, out var shortNames))
        {
            return;
        }

        var strengthAlias = GetParentheticalStrengthAlias(move.CanonicalName);
        foreach (var shortName in shortNames)
        {
            if (movement.Prefixes.Count == 0)
            {
                AddAlias(aliases, move, shortName, MoveAliasType.Colloquial);
                AddStrengthShortNameAliases(aliases, move, shortName, strengthAlias);
                continue;
            }

            foreach (var prefix in movement.Prefixes)
            {
                AddAlias(aliases, move, $"{prefix} {shortName}", MoveAliasType.Colloquial);
                if (strengthAlias is not null)
                {
                    AddAlias(aliases, move, $"{prefix} {strengthAlias.Strength} {shortName}", MoveAliasType.Derived);
                    AddAlias(aliases, move, $"{prefix} {strengthAlias.Button} {shortName}", MoveAliasType.Abbreviation);
                }
            }
        }
    }

    private void AddStrengthShortNameAliases(List<MoveAlias> aliases, Move move, string shortName, StrengthAlias? strengthAlias)
    {
        if (strengthAlias is null)
        {
            return;
        }

        AddAlias(aliases, move, $"{strengthAlias.Strength} {shortName}", MoveAliasType.Derived);
        AddAlias(aliases, move, $"{shortName} {strengthAlias.Strength}", MoveAliasType.Derived);
        AddAlias(aliases, move, $"{strengthAlias.Button} {shortName}", MoveAliasType.Abbreviation);
        AddAlias(aliases, move, $"{shortName} {strengthAlias.Button}", MoveAliasType.Abbreviation);
    }

    private void AddColloquialAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        if (normalizedCanonical == "2hk" || Normalize(move.CanonicalName).Contains("sweep", StringComparison.OrdinalIgnoreCase))
        {
            AddAlias(aliases, move, "sweep", MoveAliasType.Colloquial);
        }

        if (normalizedCanonical == "2mk")
        {
            AddAlias(aliases, move, "low forward", MoveAliasType.Colloquial);
        }
    }

    private void AddMoveSpecificColloquialAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        var characterId = Normalize(move.CharacterId);
        AddMoveSpecificColloquialAliasesForKey(aliases, move, $"{characterId}:{normalizedCanonical}");

        var normalizedBase = Normalize(GetMoveBaseName(move.CanonicalName));
        if (!string.Equals(normalizedBase, normalizedCanonical, StringComparison.Ordinal))
        {
            AddMoveSpecificColloquialAliasesForKey(aliases, move, $"{characterId}:{normalizedBase}");
        }
    }

    private void AddMoveSpecificColloquialAliasesForKey(List<MoveAlias> aliases, Move move, string key)
    {
        if (!MoveSpecificColloquialAliases.TryGetValue(key, out var colloquialAliases))
        {
            return;
        }

        var strengthAlias = GetParentheticalStrengthAlias(move.CanonicalName);
        foreach (var colloquialAlias in colloquialAliases)
        {
            AddAlias(aliases, move, colloquialAlias, MoveAliasType.Colloquial);
            AddStrengthShortNameAliases(aliases, move, colloquialAlias, strengthAlias);
        }
    }

    private void AddSuperArtAliases(
        List<MoveAlias> aliases,
        Move move,
        string normalizedCanonical,
        IReadOnlyList<Move>? characterMoves)
    {
        if (!IsSuperArt(move))
        {
            return;
        }

        var characterId = Normalize(move.CharacterId);
        var normalizedBase = Normalize(GetMoveBaseName(move.CanonicalName));
        AddConfiguredSuperArtAliases(aliases, move, characterId, normalizedCanonical, normalizedBase);

        var genericNumber = GetGenericSuperArtNumber(move, characterMoves);
        if (genericNumber is not null)
        {
            AddSuperArtNumberAliases(aliases, move, genericNumber.Value);
        }
    }

    private void AddConfiguredSuperArtAliases(
        List<MoveAlias> aliases,
        Move move,
        string characterId,
        string normalizedCanonical,
        string normalizedBase)
    {
        switch (characterId)
        {
            case "alex":
                AddAlexSuperArtAliases(aliases, move, normalizedCanonical);
                break;
            case "akuma":
                AddAkumaSuperArtAliases(aliases, move, normalizedCanonical);
                break;
            case "hugo":
                AddHugoSuperArtAliases(aliases, move, normalizedBase);
                break;
            case "ibuki":
                AddIbukiSuperArtAliases(aliases, move, normalizedCanonical);
                break;
            case "oro":
                AddOroSuperArtAliases(aliases, move, normalizedCanonical);
                break;
            case "q":
                AddQSuperArtAliases(aliases, move, normalizedCanonical);
                break;
            case "ryu":
                AddRyuSuperArtAliases(aliases, move, normalizedCanonical);
                break;
            case "twelve":
                AddTwelveSuperArtAliases(aliases, move, normalizedCanonical);
                break;
            case "urien":
                AddUrienSuperArtAliases(aliases, move, normalizedCanonical);
                break;
        }
    }

    private void AddAlexSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "hyperbomb":
                AddSuperArtNumberAliases(aliases, move, 1);
                break;
            case "reversehyperbomb":
                AddQualifiedSuperArtAliases(aliases, move, 1, "reverse", "back", "from behind");
                break;
        }
    }

    private void AddAkumaSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "messatsugouhadou":
                AddSuperArtNumberAliases(aliases, move, 1);
                break;
            case "tenmagouzankuu":
                AddAirSuperArtAliases(aliases, move, 1);
                break;
            case "messatsugoushoryuu":
                AddSuperArtNumberAliases(aliases, move, 2);
                break;
            case "messatsugourasenground":
                AddSuperArtNumberAliases(aliases, move, 3);
                break;
            case "messatsugourasenair":
                AddAirSuperArtAliases(aliases, move, 3);
                break;
        }
    }

    private void AddHugoSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedBase)
    {
        if (normalizedBase != "megatonpress")
        {
            return;
        }

        AddSuperArtNumberAliases(aliases, move, 2);
        var strengthAlias = GetParentheticalStrengthAlias(move.CanonicalName);
        if (strengthAlias is not null)
        {
            AddStrengthSuperArtAliases(aliases, move, 2, strengthAlias);
        }
    }

    private void AddIbukiSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "yoroidoushi":
                AddSuperArtNumberAliases(aliases, move, 2);
                break;
            case "missedgrabchiblast":
                AddQualifiedSuperArtAliases(aliases, move, 2, "missed", "whiffed", "whiff");
                break;
        }
    }

    private void AddOroSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "kishinriki":
                AddSuperArtNumberAliases(aliases, move, 1);
                break;
            case "groundgrab":
                AddTrailingSuperArtAliases(aliases, move, 1, "grab", "throw");
                break;
            case "jgrab":
                AddTrailingSuperArtAliases(aliases, move, 1, "air grab", "air throw");
                break;
            case "exkishinriki":
                AddQualifiedSuperArtAliases(aliases, move, 1, "ex");
                break;
            case "yagyoudama":
                AddSuperArtNumberAliases(aliases, move, 2);
                break;
            case "exyagyoudama":
                AddQualifiedSuperArtAliases(aliases, move, 2, "ex");
                break;
        }
    }

    private void AddQSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "totaldestruction":
                AddSuperArtNumberAliases(aliases, move, 3);
                break;
            case "fargrab":
                AddTrailingSuperArtAliases(aliases, move, 3, "punch");
                break;
            case "closegrab":
                AddTrailingSuperArtAliases(aliases, move, 3, "kick");
                break;
        }
    }

    private void AddRyuSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "shinshoryuken":
                AddSuperArtNumberAliases(aliases, move, 2);
                break;
            case "shinshoryukenfar":
                AddQualifiedSuperArtAliases(aliases, move, 2, "far", "missed", "whiffed", "whiff");
                break;
            case "denjinhadouken":
                AddSuperArtNumberAliases(aliases, move, 3);
                break;
        }
    }

    private void AddTwelveSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "xflat":
                AddSuperArtNumberAliases(aliases, move, 2);
                break;
            case "xflatairhit":
                AddAirSuperArtAliases(aliases, move, 2);
                break;
        }
    }

    private void AddUrienSuperArtAliases(List<MoveAlias> aliases, Move move, string normalizedCanonical)
    {
        switch (normalizedCanonical)
        {
            case "aegisreflector":
                AddSuperArtNumberAliases(aliases, move, 3);
                break;
            case "aegisreflectorex":
                AddQualifiedSuperArtAliases(aliases, move, 3, "ex");
                break;
        }
    }

    private int? GetGenericSuperArtNumber(Move move, IReadOnlyList<Move>? characterMoves)
    {
        if (characterMoves is null || IsGenericSuperArtAliasDisabled(move) || ShouldSkipGenericSuperArtAlias(move))
        {
            return null;
        }

        var characterId = Normalize(move.CharacterId);
        var primarySuperArts = characterMoves
            .Where(candidate => string.Equals(Normalize(candidate.CharacterId), characterId, StringComparison.Ordinal)
                && IsSuperArt(candidate)
                && !IsGenericSuperArtAliasDisabled(candidate)
                && !ShouldSkipGenericSuperArtAlias(candidate))
            .OrderBy(candidate => candidate.DisplayOrder ?? int.MaxValue)
            .ThenBy(candidate => candidate.CanonicalName, StringComparer.Ordinal)
            .GroupBy(candidate => Normalize(GetMoveBaseName(candidate.CanonicalName)), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(3)
            .ToArray();

        for (var index = 0; index < primarySuperArts.Length; index++)
        {
            if (string.Equals(primarySuperArts[index].Id, move.Id, StringComparison.OrdinalIgnoreCase))
            {
                return index + 1;
            }
        }

        return null;
    }

    private static bool IsGenericSuperArtAliasDisabled(Move move)
        => string.Equals(move.CharacterId, "akuma", StringComparison.OrdinalIgnoreCase);

    private bool ShouldSkipGenericSuperArtAlias(Move move)
    {
        var characterId = Normalize(move.CharacterId);
        var normalizedCanonical = Normalize(move.CanonicalName);

        return characterId switch
        {
            "alex" => normalizedCanonical is "reversehyperbomb",
            "ibuki" => normalizedCanonical is "missedgrabchiblast",
            "oro" => normalizedCanonical is "groundgrab" or "jgrab" or "exkishinriki" or "exyagyoudama",
            "q" => normalizedCanonical is "fargrab" or "closegrab",
            "ryu" => normalizedCanonical is "shinshoryukenfar",
            "twelve" => normalizedCanonical is "xflatairhit",
            "urien" => normalizedCanonical is "aegisreflectorex",
            _ => false
        };
    }

    private void AddSuperArtNumberAliases(List<MoveAlias> aliases, Move move, int number)
    {
        AddAlias(aliases, move, $"sa{number}", MoveAliasType.Abbreviation);
        AddAlias(aliases, move, $"sa {number}", MoveAliasType.Abbreviation);
        AddAlias(aliases, move, $"super art {number}", MoveAliasType.Derived);
        AddAlias(aliases, move, $"super {number}", MoveAliasType.Derived);
    }

    private void AddAirSuperArtAliases(List<MoveAlias> aliases, Move move, int number)
    {
        AddQualifiedSuperArtAliases(aliases, move, number, "air", "jp", "jumping", "jump");
        AddTrailingSuperArtAliases(aliases, move, number, "air");
    }

    private void AddQualifiedSuperArtAliases(List<MoveAlias> aliases, Move move, int number, params string[] qualifiers)
    {
        foreach (var qualifier in qualifiers)
        {
            AddAlias(aliases, move, $"{qualifier} sa{number}", MoveAliasType.Colloquial);
            AddAlias(aliases, move, $"sa{number} {qualifier}", MoveAliasType.Colloquial);
            AddAlias(aliases, move, $"{qualifier} super art {number}", MoveAliasType.Colloquial);
        }
    }

    private void AddTrailingSuperArtAliases(List<MoveAlias> aliases, Move move, int number, params string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            AddAlias(aliases, move, $"sa{number} {suffix}", MoveAliasType.Colloquial);
            AddAlias(aliases, move, $"{suffix} sa{number}", MoveAliasType.Colloquial);
            AddAlias(aliases, move, $"super art {number} {suffix}", MoveAliasType.Colloquial);
        }
    }

    private void AddStrengthSuperArtAliases(List<MoveAlias> aliases, Move move, int number, StrengthAlias strengthAlias)
    {
        AddQualifiedSuperArtAliases(aliases, move, number, strengthAlias.Strength, strengthAlias.Button);
        AddTrailingSuperArtAliases(aliases, move, number, strengthAlias.Strength, strengthAlias.Button);
    }

    private void AddAlias(List<MoveAlias> aliases, Move move, string alias, MoveAliasType aliasType)
    {
        var normalized = Normalize(alias);
        if (normalized.Length == 0)
        {
            return;
        }

        aliases.Add(new MoveAlias
        {
            Id = $"{move.Id}:{normalized}",
            MoveId = move.Id,
            Alias = alias,
            AliasType = aliasType,
            NormalizedAlias = normalized
        });
    }

    private static string ApplyPositionPrefix(string compact)
    {
        if (compact.StartsWith("downback", StringComparison.Ordinal))
        {
            return "1" + compact["downback".Length..];
        }

        if (compact.StartsWith('1') && compact.Length > 1)
        {
            return compact;
        }

        if (compact.StartsWith("db", StringComparison.Ordinal) && compact.Length > 2)
        {
            return "1" + compact[2..];
        }

        if (compact.StartsWith("downforward", StringComparison.Ordinal))
        {
            return "3" + compact["downforward".Length..];
        }

        if (compact.StartsWith('3') && compact.Length > 1)
        {
            return compact;
        }

        if (compact.StartsWith("df", StringComparison.Ordinal) && compact.Length > 2)
        {
            return "3" + compact[2..];
        }

        if (compact.StartsWith("towards", StringComparison.Ordinal))
        {
            return "6" + compact["towards".Length..];
        }

        if (compact.StartsWith("toward", StringComparison.Ordinal))
        {
            return "6" + compact["toward".Length..];
        }

        if (compact.StartsWith('6') && compact.Length > 1)
        {
            return compact;
        }

        if (compact.StartsWith("back", StringComparison.Ordinal) && IsDirectionalFiveSuffix(compact["back".Length..]))
        {
            return "5" + compact["back".Length..];
        }

        if (compact.StartsWith("crouching", StringComparison.Ordinal))
        {
            return "2" + compact["crouching".Length..];
        }

        if (compact.StartsWith("crouch", StringComparison.Ordinal))
        {
            return "2" + compact["crouch".Length..];
        }

        if (compact.StartsWith("cr", StringComparison.Ordinal) && IsButtonSuffix(compact[2..]))
        {
            return "2" + compact[2..];
        }

        if (compact.StartsWith('c') && compact.Length > 1 && IsButtonSuffix(compact[1..]))
        {
            return "2" + compact[1..];
        }

        if (compact.StartsWith("standing", StringComparison.Ordinal))
        {
            return "5" + compact["standing".Length..];
        }

        if (compact.StartsWith("stand", StringComparison.Ordinal))
        {
            return "5" + compact["stand".Length..];
        }

        if (compact.StartsWith("st", StringComparison.Ordinal) && IsButtonSuffix(compact[2..]))
        {
            return "5" + compact[2..];
        }

        if (compact.StartsWith('s') && compact.Length > 1 && IsButtonSuffix(compact[1..]))
        {
            return "5" + compact[1..];
        }

        if (compact.StartsWith("jumping", StringComparison.Ordinal))
        {
            return "j" + compact["jumping".Length..];
        }

        if (compact.StartsWith("jump", StringComparison.Ordinal))
        {
            return "j" + compact["jump".Length..];
        }

        if (compact.StartsWith("air", StringComparison.Ordinal))
        {
            return "j" + compact["air".Length..];
        }

        if (compact.StartsWith("jp", StringComparison.Ordinal) && compact.Length > 2)
        {
            return "j" + compact[2..];
        }

        if (compact.StartsWith('j') && compact.Length > 1 && IsButtonSuffix(compact[1..]))
        {
            return "j" + compact[1..];
        }

        return compact;
    }

    private static string ApplyDirectionalPhraseAliases(string value)
    {
        value = DownBackPhrase().Replace(value, "1");
        value = DownForwardPhrase().Replace(value, "3");
        return value;
    }

    private static string ApplyMotionPhraseAliases(string value)
    {
        value = DoubleQuarterCircleForwardPhrase().Replace(value, "236236");
        value = DoubleQuarterCircleBackPhrase().Replace(value, "214214");
        value = QuarterCircleForwardPhrase().Replace(value, "236");
        value = QuarterCircleBackPhrase().Replace(value, "214");
        value = HalfCircleForwardPhrase().Replace(value, "41236");
        value = HalfCircleBackPhrase().Replace(value, "63214");
        value = DragonPunchMotionPhrase().Replace(value, "dpm");
        value = DragonPunchBeforeButtonPhrase().Replace(value, "dpm");
        return value;
    }

    private static string GetMoveBaseName(string canonicalName)
    {
        var match = ParentheticalVariantName().Match(canonicalName);
        return match.Success ? match.Groups["baseName"].Value.Trim() : canonicalName.Trim();
    }

    private StrengthAlias? GetParentheticalStrengthAlias(string canonicalName)
    {
        var match = ParentheticalVariantName().Match(canonicalName);
        if (!match.Success)
        {
            return null;
        }

        var indicator = Normalize(match.Groups["indicator"].Value);
        return ParentheticalStrengthAliases.TryGetValue(indicator, out var strengthAlias) ? strengthAlias : null;
    }

    private static MovementPrefixResult StripMovementPrefix(string baseName)
    {
        foreach (var prefix in MovementPrefixes)
        {
            if (!baseName.StartsWith(prefix.RawPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var strippedBaseName = baseName[prefix.RawPrefix.Length..].Trim();
            if (strippedBaseName.Length == 0)
            {
                return new MovementPrefixResult(baseName, []);
            }

            return new MovementPrefixResult(strippedBaseName, prefix.Aliases);
        }

        return new MovementPrefixResult(baseName, []);
    }

    private static MotionParts? TryGetMotionParts(string normalizedMotion)
    {
        foreach (var motion in MotionNotationPrefixes)
        {
            if (normalizedMotion.StartsWith(motion, StringComparison.Ordinal))
            {
                return new MotionParts(motion, normalizedMotion[motion.Length..]);
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractMotionButtons(string suffix)
    {
        var buttons = new List<string>();
        var index = 0;
        while (index < suffix.Length)
        {
            var button = MotionButtonTokens.FirstOrDefault(token => suffix[index..].StartsWith(token, StringComparison.Ordinal));
            if (button is null)
            {
                index++;
                continue;
            }

            buttons.Add(button);
            index += button.Length;
        }

        return buttons;
    }

    private static IReadOnlyList<string> SelectMotionButtonAliases(
        IReadOnlyList<string> buttons,
        StrengthAlias? parentheticalStrength)
    {
        if (parentheticalStrength is null)
        {
            return buttons;
        }

        if (buttons.Count == 0
            || buttons.Contains(parentheticalStrength.Button, StringComparer.Ordinal)
            || buttons.Contains(GetGenericButton(parentheticalStrength.Button), StringComparer.Ordinal))
        {
            return [parentheticalStrength.Button];
        }

        return buttons;
    }

    private static string GetGenericButton(string button)
        => button.EndsWith('p') ? "p" : button.EndsWith('k') ? "k" : string.Empty;

    private static bool IsButtonSuffix(string value)
    {
        return value is "lp" or "mp" or "hp" or "lk" or "mk" or "hk";
    }

    private static bool IsDirectionalFiveSuffix(string value)
        => IsButtonSuffix(value) || value.StartsWith("sa", StringComparison.Ordinal);

    private static bool IsSuperArt(Move move)
        => string.Equals(move.Section, "SuperArts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(move.Section, "Super Arts", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"\bdown\s*(?:\+|/|-)?\s*back\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DownBackPhrase();

    [GeneratedRegex(@"\bdown\s*(?:\+|/|-)?\s*forward\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DownForwardPhrase();

    [GeneratedRegex(@"\b(?:(?:qcf|quarter[\s-]+circle[\s-]+forward)\s*x\s*2|2\s*x\s*(?:qcf|quarter[\s-]+circle[\s-]+forward)|double\s*(?:qcf|quarter[\s-]+circle[\s-]+forward))\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DoubleQuarterCircleForwardPhrase();

    [GeneratedRegex(@"\b(?:(?:qcb|quarter[\s-]+circle[\s-]+back)\s*x\s*2|2\s*x\s*(?:qcb|quarter[\s-]+circle[\s-]+back)|double\s*(?:qcb|quarter[\s-]+circle[\s-]+back))\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DoubleQuarterCircleBackPhrase();

    [GeneratedRegex(@"\b(?:qcf|quarter[\s-]+circle[\s-]+forward)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex QuarterCircleForwardPhrase();

    [GeneratedRegex(@"\b(?:qcb|quarter[\s-]+circle[\s-]+back)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex QuarterCircleBackPhrase();

    [GeneratedRegex(@"\b(?:hcf|half[\s-]+circle[\s-]+forward)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex HalfCircleForwardPhrase();

    [GeneratedRegex(@"\b(?:hcb|half[\s-]+circle[\s-]+back)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex HalfCircleBackPhrase();

    [GeneratedRegex(@"\bdp\s*motion\b|\bdpm\b", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DragonPunchMotionPhrase();

    [GeneratedRegex(@"\bdp\b(?=\s*\+?\s*(?:lp|mp|hp|lk|mk|hk|jab|strong|fierce|short|forward|roundhouse|rh|light\s+punch|medium\s+punch|heavy\s+punch|light\s+kick|medium\s+kick|heavy\s+kick)\b)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DragonPunchBeforeButtonPhrase();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonLookupCharacters();

    [GeneratedRegex(@"^\s*(?<baseName>.+?)\s*\((?<indicator>[^)]+)\)\s*$", RegexOptions.Compiled)]
    private static partial Regex ParentheticalVariantName();

    private sealed record StrengthAlias(string Strength, string Button);
    private sealed record MovementPrefix(string RawPrefix, IReadOnlyList<string> Aliases);
    private sealed record MovementPrefixResult(string BaseName, IReadOnlyList<string> Prefixes);
    private readonly record struct MotionParts(string Motion, string Suffix);

    private static readonly IReadOnlyList<MovementPrefix> MovementPrefixes =
    [
        new MovementPrefix("jumping ", ["jumping", "jump", "jp", "j", "air"]),
        new MovementPrefix("jump ", ["jumping", "jump", "jp", "j", "air"]),
        new MovementPrefix("air ", ["air", "jumping", "jump", "jp", "j"])
    ];

    private static readonly IReadOnlyList<string> MotionNotationPrefixes =
    [
        "236236",
        "214214",
        "41236",
        "63214",
        "236",
        "214",
        "360",
        "dpm"
    ];

    private static readonly IReadOnlyList<string> MotionButtonTokens =
    [
        "lp",
        "mp",
        "hp",
        "lk",
        "mk",
        "hk",
        "p",
        "k"
    ];
}
