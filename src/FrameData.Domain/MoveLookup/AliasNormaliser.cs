using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public sealed class AliasNormaliser
{
    private readonly SuperArtAliasProvider _superArtAliasProvider = new();

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
        ["oro:indecentexposure"] = ["🥜"],
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
        ["spiralddt"] = ["ddt"],
        ["tatsumakisenpuukyaku"] = ["tatsu"],
        ["tetsuzankou"] = ["shoulder"],
        ["tourouzan"] = ["slashes", "rekkas", "mantis slash"],
        ["violencekneedrop"] = ["knee drop", "knee"],
        ["shipuujinraikyaku"] = ["shipu", "shippu"],
        ["tengustones"] = ["stones"],
        ["universaloverhead"] = ["uoh", "leap"],
        ["yagyoudama"] = ["yagyou", "booger"],
        ["zenpoutenshin"] = ["zenpo", "command grab", "cmd grab"],
        ["zesshouhohou"] = ["lunge punch"]
    };

    public string Normalise(string input) => AliasTextNormaliser.Normalise(input);

    public IReadOnlyList<MoveAlias> CreateAliases(Move move, IReadOnlyList<Move>? characterMoves = null)
    {
        var aliases = new List<MoveAlias>();
        AddAlias(aliases, move, move.CanonicalName, MoveAliasType.Canonical);

        var normalisedCanonical = Normalise(move.CanonicalName);
        AddDerivedNotationAliases(aliases, move, normalisedCanonical);
        AddCloseNormalAliases(aliases, move, normalisedCanonical);
        AddSpecialMoveStrengthAliases(aliases, move);
        AddMotionAliases(aliases, move);
        AddKnownMoveShortNameAliases(aliases, move);
        AddMoveSpecificColloquialAliases(aliases, move, normalisedCanonical);
        AddColloquialAliases(aliases, move, normalisedCanonical);
        foreach (var superArtAlias in _superArtAliasProvider.CreateAliases(move, characterMoves))
        {
            AddAlias(aliases, move, superArtAlias.Alias, superArtAlias.AliasType);
        }

        return aliases
            .GroupBy(alias => alias.NormalisedAlias, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(alias => alias.AliasType is MoveAliasType.Canonical ? 1 : 0)
                .ThenBy(alias => alias.AliasType)
                .First())
            .ToArray();
    }

    private void AddDerivedNotationAliases(List<MoveAlias> aliases, Move move, string normalisedCanonical)
    {
        if (normalisedCanonical.Length < 3)
        {
            return;
        }

        var button = normalisedCanonical[1..];
        switch (normalisedCanonical[0])
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
            case '4':
                AddAlias(aliases, move, $"b.{button}", MoveAliasType.Abbreviation);
                AddAlias(aliases, move, $"back {button}", MoveAliasType.Numpad);
                break;
            case '6':
                AddAlias(aliases, move, $"f.{button}", MoveAliasType.Abbreviation);
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

    private void AddCloseNormalAliases(List<MoveAlias> aliases, Move move, string normalisedCanonical)
    {
        var button = normalisedCanonical.Length switch
        {
            2 when AliasTextNormaliser.IsButtonSuffix(normalisedCanonical) => normalisedCanonical,
            3 when normalisedCanonical.StartsWith('5') && AliasTextNormaliser.IsButtonSuffix(normalisedCanonical[1..]) => normalisedCanonical[1..],
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
        if (!AliasTextNormaliser.TryGetParentheticalVariant(move.CanonicalName, out var baseName))
        {
            return;
        }

        var strengthAlias = AliasTextNormaliser.GetParentheticalStrengthAlias(move.CanonicalName);
        if (strengthAlias is null)
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

        var normalisedMotion = Normalise(move.Motion);
        AddAlias(aliases, move, move.Motion, MoveAliasType.Numpad);

        var motionParts = AliasTextNormaliser.TryGetMotionParts(normalisedMotion);
        if (motionParts is null)
        {
            return;
        }

        AddAlias(aliases, move, motionParts.Value.Motion, MoveAliasType.Numpad);

        var parentheticalStrength = AliasTextNormaliser.GetParentheticalStrengthAlias(move.CanonicalName);
        var buttons = AliasTextNormaliser.ExtractMotionButtons(motionParts.Value.Suffix);
        foreach (var button in AliasTextNormaliser.SelectMotionButtonAliases(buttons, parentheticalStrength))
        {
            AddAlias(aliases, move, $"{motionParts.Value.Motion}{button}", MoveAliasType.Numpad);
        }
    }

    private void AddKnownMoveShortNameAliases(List<MoveAlias> aliases, Move move)
    {
        var baseName = AliasTextNormaliser.GetMoveBaseName(move.CanonicalName);
        var movement = AliasTextNormaliser.StripMovementPrefix(baseName);
        var normalisedBase = Normalise(movement.BaseName);

        if (!KnownMoveShortNameAliases.TryGetValue(normalisedBase, out var shortNames))
        {
            return;
        }

        var strengthAlias = AliasTextNormaliser.GetParentheticalStrengthAlias(move.CanonicalName);
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

    private void AddColloquialAliases(List<MoveAlias> aliases, Move move, string normalisedCanonical)
    {
        if (normalisedCanonical == "2hk" || Normalise(move.CanonicalName).Contains("sweep", StringComparison.OrdinalIgnoreCase))
        {
            AddAlias(aliases, move, "sweep", MoveAliasType.Colloquial);
        }

        if (normalisedCanonical == "2mk")
        {
            AddAlias(aliases, move, "low forward", MoveAliasType.Colloquial);
        }
    }

    private void AddMoveSpecificColloquialAliases(List<MoveAlias> aliases, Move move, string normalisedCanonical)
    {
        var characterId = Normalise(move.CharacterId);
        AddMoveSpecificColloquialAliasesForKey(aliases, move, $"{characterId}:{normalisedCanonical}");

        var normalisedBase = Normalise(AliasTextNormaliser.GetMoveBaseName(move.CanonicalName));
        if (!string.Equals(normalisedBase, normalisedCanonical, StringComparison.Ordinal))
        {
            AddMoveSpecificColloquialAliasesForKey(aliases, move, $"{characterId}:{normalisedBase}");
        }
    }

    private void AddMoveSpecificColloquialAliasesForKey(List<MoveAlias> aliases, Move move, string key)
    {
        if (!MoveSpecificColloquialAliases.TryGetValue(key, out var colloquialAliases))
        {
            return;
        }

        var strengthAlias = AliasTextNormaliser.GetParentheticalStrengthAlias(move.CanonicalName);
        foreach (var colloquialAlias in colloquialAliases)
        {
            AddAlias(aliases, move, colloquialAlias, MoveAliasType.Colloquial);
            AddStrengthShortNameAliases(aliases, move, colloquialAlias, strengthAlias);
        }
    }

    private void AddAlias(List<MoveAlias> aliases, Move move, string alias, MoveAliasType aliasType)
    {
        var normalised = Normalise(alias);
        if (normalised.Length == 0)
        {
            return;
        }

        aliases.Add(new MoveAlias
        {
            Id = $"{move.Id}:{normalised}",
            MoveId = move.Id,
            Alias = alias,
            AliasType = aliasType,
            NormalisedAlias = normalised
        });
    }
}
