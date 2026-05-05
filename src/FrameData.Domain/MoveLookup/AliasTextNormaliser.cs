using System.Text.RegularExpressions;
using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

internal static partial class AliasTextNormaliser
{
    private static readonly IReadOnlyDictionary<string, StrengthAlias> ParentheticalStrengthAliases =
        new Dictionary<string, StrengthAlias>(StringComparer.OrdinalIgnoreCase)
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

    private static readonly IReadOnlyDictionary<string, string> AttackTermAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

    public static string Normalise(string input)
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

    public static string GetMoveBaseName(string canonicalName)
    {
        var match = ParentheticalVariantName().Match(canonicalName);
        return match.Success ? match.Groups["baseName"].Value.Trim() : canonicalName.Trim();
    }

    public static StrengthAlias? GetParentheticalStrengthAlias(string canonicalName)
    {
        var match = ParentheticalVariantName().Match(canonicalName);
        if (!match.Success)
        {
            return null;
        }

        var indicator = Normalise(match.Groups["indicator"].Value);
        return ParentheticalStrengthAliases.TryGetValue(indicator, out var strengthAlias) ? strengthAlias : null;
    }

    public static bool TryGetParentheticalVariant(string canonicalName, out string baseName)
    {
        var match = ParentheticalVariantName().Match(canonicalName);
        if (!match.Success)
        {
            baseName = string.Empty;
            return false;
        }

        baseName = match.Groups["baseName"].Value.Trim();
        return true;
    }

    public static MovementPrefixResult StripMovementPrefix(string baseName)
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

    public static MotionParts? TryGetMotionParts(string normalisedMotion)
    {
        foreach (var motion in MotionNotationPrefixes)
        {
            if (normalisedMotion.StartsWith(motion, StringComparison.Ordinal))
            {
                return new MotionParts(motion, normalisedMotion[motion.Length..]);
            }
        }

        return null;
    }

    public static IReadOnlyList<string> ExtractMotionButtons(string suffix)
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

    public static IReadOnlyList<string> SelectMotionButtonAliases(
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

    public static bool IsButtonSuffix(string value)
        => value is "lp" or "mp" or "hp" or "lk" or "mk" or "hk";

    public static bool IsSuperArt(Move move)
        => string.Equals(move.Section, "SuperArts", StringComparison.OrdinalIgnoreCase)
            || string.Equals(move.Section, "Super Arts", StringComparison.OrdinalIgnoreCase);

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

        if (compact.StartsWith("back", StringComparison.Ordinal) && IsDirectionalFourSuffix(compact["back".Length..]))
        {
            return "4" + compact["back".Length..];
        }

        if (compact.StartsWith('b') && compact.Length > 1 && IsButtonSuffix(compact[1..]))
        {
            return "4" + compact[1..];
        }

        if (compact.StartsWith('f') && compact.Length > 1 && IsButtonSuffix(compact[1..]))
        {
            return "6" + compact[1..];
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

    private static string GetGenericButton(string button)
        => button.EndsWith('p') ? "p" : button.EndsWith('k') ? "k" : string.Empty;

    private static bool IsDirectionalFourSuffix(string value)
        => IsButtonSuffix(value) || value.StartsWith("sa", StringComparison.Ordinal);

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
}

internal sealed record StrengthAlias(string Strength, string Button);
internal sealed record MovementPrefix(string RawPrefix, IReadOnlyList<string> Aliases);
internal sealed record MovementPrefixResult(string BaseName, IReadOnlyList<string> Prefixes);
internal readonly record struct MotionParts(string Motion, string Suffix);
