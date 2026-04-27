using System.Text.RegularExpressions;
using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public sealed partial class AliasNormalizer
{
    private static readonly IReadOnlyDictionary<string, string[]> MoveSpecificColloquialAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["dudley:2hk"] = ["kanipan"],
        ["makoto:hayate"] = ["chesto"]
    };

    private static readonly IReadOnlyDictionary<string, string[]> KnownMoveShortNameAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["tatsumakisenpuukyaku"] = ["tatsu"],
        ["shipuujinraikyaku"] = ["shipu", "shippu"],
        ["universaloverhead"] = ["uoh"]
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
        ["hk"] = new StrengthAlias("heavy", "hk")
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
        ["low forward"] = "2mk"
    };

    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var value = input.Trim().ToLowerInvariant();
        foreach (var alias in AttackTermAliases.OrderByDescending(alias => alias.Key.Length))
        {
            value = Regex.Replace(value, $@"\b{Regex.Escape(alias.Key)}\b", alias.Value, RegexOptions.IgnoreCase);
        }

        var compact = NonLookupCharacters().Replace(value, string.Empty);
        return ApplyPositionPrefix(compact);
    }

    public IReadOnlyList<MoveAlias> CreateAliases(Move move)
    {
        var aliases = new List<MoveAlias>();
        AddAlias(aliases, move, move.CanonicalName, MoveAliasType.Canonical);

        var normalizedCanonical = Normalize(move.CanonicalName);
        AddDerivedNotationAliases(aliases, move, normalizedCanonical);
        AddCloseNormalAliases(aliases, move, normalizedCanonical);
        AddSpecialMoveStrengthAliases(aliases, move);
        AddKnownMoveShortNameAliases(aliases, move);
        AddMoveSpecificColloquialAliases(aliases, move, normalizedCanonical);
        AddColloquialAliases(aliases, move, normalizedCanonical);

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
        var key = $"{Normalize(move.CharacterId)}:{normalizedCanonical}";
        if (!MoveSpecificColloquialAliases.TryGetValue(key, out var colloquialAliases))
        {
            return;
        }

        foreach (var colloquialAlias in colloquialAliases)
        {
            AddAlias(aliases, move, colloquialAlias, MoveAliasType.Colloquial);
        }
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
        if (compact.StartsWith("crouching", StringComparison.Ordinal))
        {
            return "2" + compact["crouching".Length..];
        }

        if (compact.StartsWith("crouch", StringComparison.Ordinal))
        {
            return "2" + compact["crouch".Length..];
        }

        if (compact.StartsWith("cr", StringComparison.Ordinal))
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

        if (compact.StartsWith("st", StringComparison.Ordinal))
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

    private static bool IsButtonSuffix(string value)
    {
        return value is "lp" or "mp" or "hp" or "lk" or "mk" or "hk";
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonLookupCharacters();

    [GeneratedRegex(@"^\s*(?<baseName>.+?)\s*\((?<indicator>[^)]+)\)\s*$", RegexOptions.Compiled)]
    private static partial Regex ParentheticalVariantName();

    private sealed record StrengthAlias(string Strength, string Button);
    private sealed record MovementPrefix(string RawPrefix, IReadOnlyList<string> Aliases);
    private sealed record MovementPrefixResult(string BaseName, IReadOnlyList<string> Prefixes);

    private static readonly IReadOnlyList<MovementPrefix> MovementPrefixes =
    [
        new MovementPrefix("jumping ", ["jumping", "jump", "jp", "j", "air"]),
        new MovementPrefix("jump ", ["jumping", "jump", "jp", "j", "air"]),
        new MovementPrefix("air ", ["air", "jumping", "jump", "jp", "j"])
    ];
}
