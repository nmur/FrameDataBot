using System.Text.RegularExpressions;
using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public sealed partial class AliasNormalizer
{
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
        }
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

        return compact;
    }

    private static bool IsButtonSuffix(string value)
    {
        return value is "lp" or "mp" or "hp" or "lk" or "mk" or "hk";
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonLookupCharacters();
}
