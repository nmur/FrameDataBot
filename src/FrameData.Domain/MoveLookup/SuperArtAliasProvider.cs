using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

internal sealed class SuperArtAliasProvider
{
    public IReadOnlyList<UnnormalisedMoveAlias> CreateAliases(Move move, IReadOnlyList<Move>? characterMoves)
    {
        if (!AliasTextNormaliser.IsSuperArt(move))
        {
            return [];
        }

        var aliases = new List<UnnormalisedMoveAlias>();
        var characterId = AliasTextNormaliser.Normalise(move.CharacterId);
        var normalisedCanonical = AliasTextNormaliser.Normalise(move.CanonicalName);
        var normalisedBase = AliasTextNormaliser.Normalise(AliasTextNormaliser.GetMoveBaseName(move.CanonicalName));
        AddConfiguredSuperArtAliases(aliases, move, characterId, normalisedCanonical, normalisedBase);

        var genericNumber = GetGenericSuperArtNumber(move, characterMoves);
        if (genericNumber is not null)
        {
            AddSuperArtNumberAliases(aliases, genericNumber.Value);
        }

        return aliases;
    }

    private static void AddConfiguredSuperArtAliases(
        List<UnnormalisedMoveAlias> aliases,
        Move move,
        string characterId,
        string normalisedCanonical,
        string normalisedBase)
    {
        switch (characterId)
        {
            case "alex":
                AddAlexSuperArtAliases(aliases, normalisedCanonical);
                break;
            case "akuma":
                AddAkumaSuperArtAliases(aliases, normalisedCanonical);
                break;
            case "hugo":
                AddHugoSuperArtAliases(aliases, move, normalisedBase);
                break;
            case "ibuki":
                AddIbukiSuperArtAliases(aliases, normalisedCanonical);
                break;
            case "oro":
                AddOroSuperArtAliases(aliases, normalisedCanonical);
                break;
            case "q":
                AddQSuperArtAliases(aliases, normalisedCanonical);
                break;
            case "ryu":
                AddRyuSuperArtAliases(aliases, normalisedCanonical);
                break;
            case "twelve":
                AddTwelveSuperArtAliases(aliases, normalisedCanonical);
                break;
            case "urien":
                AddUrienSuperArtAliases(aliases, normalisedCanonical);
                break;
        }
    }

    private static void AddAlexSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "hyperbomb":
                AddSuperArtNumberAliases(aliases, 1);
                break;
            case "reversehyperbomb":
                AddQualifiedSuperArtAliases(aliases, 1, "reverse", "back", "from behind");
                break;
        }
    }

    private static void AddAkumaSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "messatsugouhadou":
                AddSuperArtNumberAliases(aliases, 1);
                break;
            case "tenmagouzankuu":
                AddAirSuperArtAliases(aliases, 1);
                break;
            case "messatsugoushoryuu":
                AddSuperArtNumberAliases(aliases, 2);
                break;
            case "messatsugourasenground":
                AddSuperArtNumberAliases(aliases, 3);
                break;
            case "messatsugourasenair":
                AddAirSuperArtAliases(aliases, 3);
                break;
        }
    }

    private static void AddHugoSuperArtAliases(List<UnnormalisedMoveAlias> aliases, Move move, string normalisedBase)
    {
        if (normalisedBase != "megatonpress")
        {
            return;
        }

        AddSuperArtNumberAliases(aliases, 2);
        var strengthAlias = AliasTextNormaliser.GetParentheticalStrengthAlias(move.CanonicalName);
        if (strengthAlias is not null)
        {
            AddStrengthSuperArtAliases(aliases, 2, strengthAlias);
        }
    }

    private static void AddIbukiSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "yoroidoushi":
                AddSuperArtNumberAliases(aliases, 2);
                break;
            case "missedgrabchiblast":
                AddQualifiedSuperArtAliases(aliases, 2, "missed", "whiffed", "whiff");
                break;
        }
    }

    private static void AddOroSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "kishinriki":
                AddSuperArtNumberAliases(aliases, 1);
                break;
            case "groundgrab":
                AddTrailingSuperArtAliases(aliases, 1, "grab", "throw");
                break;
            case "jgrab":
                AddTrailingSuperArtAliases(aliases, 1, "air grab", "air throw");
                break;
            case "exkishinriki":
                AddQualifiedSuperArtAliases(aliases, 1, "ex");
                break;
            case "yagyoudama":
                AddSuperArtNumberAliases(aliases, 2);
                break;
            case "exyagyoudama":
                AddQualifiedSuperArtAliases(aliases, 2, "ex");
                break;
        }
    }

    private static void AddQSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "totaldestruction":
                AddSuperArtNumberAliases(aliases, 3);
                break;
            case "fargrab":
                AddTrailingSuperArtAliases(aliases, 3, "punch");
                break;
            case "closegrab":
                AddTrailingSuperArtAliases(aliases, 3, "kick");
                break;
        }
    }

    private static void AddRyuSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "shinshoryuken":
                AddSuperArtNumberAliases(aliases, 2);
                break;
            case "shinshoryukenfar":
                AddQualifiedSuperArtAliases(aliases, 2, "far", "missed", "whiffed", "whiff");
                break;
            case "denjinhadouken":
                AddSuperArtNumberAliases(aliases, 3);
                break;
        }
    }

    private static void AddTwelveSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "xflat":
                AddSuperArtNumberAliases(aliases, 2);
                break;
            case "xflatairhit":
                AddAirSuperArtAliases(aliases, 2);
                break;
        }
    }

    private static void AddUrienSuperArtAliases(List<UnnormalisedMoveAlias> aliases, string normalisedCanonical)
    {
        switch (normalisedCanonical)
        {
            case "aegisreflector":
                AddSuperArtNumberAliases(aliases, 3);
                break;
            case "aegisreflectorex":
                AddQualifiedSuperArtAliases(aliases, 3, "ex");
                break;
        }
    }

    private static int? GetGenericSuperArtNumber(Move move, IReadOnlyList<Move>? characterMoves)
    {
        if (characterMoves is null || IsGenericSuperArtAliasDisabled(move) || ShouldSkipGenericSuperArtAlias(move))
        {
            return null;
        }

        var characterId = AliasTextNormaliser.Normalise(move.CharacterId);
        var primarySuperArts = characterMoves
            .Where(candidate => string.Equals(AliasTextNormaliser.Normalise(candidate.CharacterId), characterId, StringComparison.Ordinal)
                && AliasTextNormaliser.IsSuperArt(candidate)
                && !IsGenericSuperArtAliasDisabled(candidate)
                && !ShouldSkipGenericSuperArtAlias(candidate))
            .OrderBy(candidate => candidate.DisplayOrder ?? int.MaxValue)
            .ThenBy(candidate => candidate.CanonicalName, StringComparer.Ordinal)
            .GroupBy(candidate => AliasTextNormaliser.Normalise(AliasTextNormaliser.GetMoveBaseName(candidate.CanonicalName)), StringComparer.OrdinalIgnoreCase)
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

    private static bool ShouldSkipGenericSuperArtAlias(Move move)
    {
        var characterId = AliasTextNormaliser.Normalise(move.CharacterId);
        var normalisedCanonical = AliasTextNormaliser.Normalise(move.CanonicalName);

        return characterId switch
        {
            "alex" => normalisedCanonical is "reversehyperbomb",
            "ibuki" => normalisedCanonical is "missedgrabchiblast",
            "oro" => normalisedCanonical is "groundgrab" or "jgrab" or "exkishinriki" or "exyagyoudama",
            "q" => normalisedCanonical is "fargrab" or "closegrab",
            "ryu" => normalisedCanonical is "shinshoryukenfar",
            "twelve" => normalisedCanonical is "xflatairhit",
            "urien" => normalisedCanonical is "aegisreflectorex",
            _ => false
        };
    }

    private static void AddSuperArtNumberAliases(List<UnnormalisedMoveAlias> aliases, int number)
    {
        AddAlias(aliases, $"sa{number}", MoveAliasType.Abbreviation);
        AddAlias(aliases, $"sa {number}", MoveAliasType.Abbreviation);
        AddAlias(aliases, $"super art {number}", MoveAliasType.Derived);
        AddAlias(aliases, $"super {number}", MoveAliasType.Derived);
    }

    private static void AddAirSuperArtAliases(List<UnnormalisedMoveAlias> aliases, int number)
    {
        AddQualifiedSuperArtAliases(aliases, number, "air", "jp", "jumping", "jump");
        AddTrailingSuperArtAliases(aliases, number, "air");
    }

    private static void AddQualifiedSuperArtAliases(
        List<UnnormalisedMoveAlias> aliases,
        int number,
        params string[] qualifiers)
    {
        foreach (var qualifier in qualifiers)
        {
            AddAlias(aliases, $"{qualifier} sa{number}", MoveAliasType.Colloquial);
            AddAlias(aliases, $"sa{number} {qualifier}", MoveAliasType.Colloquial);
            AddAlias(aliases, $"{qualifier} super art {number}", MoveAliasType.Colloquial);
        }
    }

    private static void AddTrailingSuperArtAliases(
        List<UnnormalisedMoveAlias> aliases,
        int number,
        params string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            AddAlias(aliases, $"sa{number} {suffix}", MoveAliasType.Colloquial);
            AddAlias(aliases, $"{suffix} sa{number}", MoveAliasType.Colloquial);
            AddAlias(aliases, $"super art {number} {suffix}", MoveAliasType.Colloquial);
        }
    }

    private static void AddStrengthSuperArtAliases(
        List<UnnormalisedMoveAlias> aliases,
        int number,
        StrengthAlias strengthAlias)
    {
        AddQualifiedSuperArtAliases(aliases, number, strengthAlias.Strength, strengthAlias.Button);
        AddTrailingSuperArtAliases(aliases, number, strengthAlias.Strength, strengthAlias.Button);
    }

    private static void AddAlias(List<UnnormalisedMoveAlias> aliases, string alias, MoveAliasType aliasType)
        => aliases.Add(new UnnormalisedMoveAlias(alias, aliasType));
}

internal readonly record struct UnnormalisedMoveAlias(string Alias, MoveAliasType AliasType);
