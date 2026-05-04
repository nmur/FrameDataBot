using FrameData.Domain.Moves;

namespace FrameData.Api.Responses;

public static class GameRestaurantUrlResolver
{
    private const string BaseUrl = "http://gere.stars.ne.jp/01_3rd/kouryaku";

    private static readonly IReadOnlyDictionary<string, GameRestaurantCharacterPage> CharacterPages =
        CreateCharacterPageMap(
            Character("alex"),
            Character("ryu"),
            Character("yun"),
            Character("dudley"),
            Character("necro"),
            Character("hugo"),
            Character("ibuki"),
            Character("elena"),
            Character("oro"),
            Character("yang"),
            Character("ken"),
            Character("sean"),
            Character("urien"),
            Character("akuma", siteKey: "gouki"),
            Character("gill"),
            Character("chun-li", siteKey: "chunli", aliases: ["chun", "chun li"]),
            Character("makoto"),
            Character("q"),
            Character("twelve", siteKey: "12"),
            Character("remy"));

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CharacterSpecificPageCodes =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["chun-li"] = MovePages(
                Page("h1", "kikoken", "kikouken", "kikkoken")),
            ["makoto"] = MovePages(
                Page("h1", "hayate")),
            ["ryu"] = MovePages(
                Page("h1", "hadouken"),
                Page("h2", "shoryuken"),
                Page("h3", "tatsumakisenpuukyaku"),
                Page("h4", "airtatsumakisenpuukyaku"),
                Page("h5", "joudansokutougeri"),
                Page("sa1", "shinkuhadouken"),
                Page("sa2", "shinshoryuken"),
                Page("sa3", "denjinhadouken"))
        };

    private static readonly IReadOnlyDictionary<string, GameRestaurantButtonPage> ButtonPages =
        CreateButtonPageMap(
            Button("lp", "sp", "jab"),
            Button("mp", "mp", "strong"),
            Button("hp", "lp", "fierce"),
            Button("lk", "sk", "short"),
            Button("mk", "mk", "forward"),
            Button("hk", "lk", "roundhouse", "rh"));

    public static string? Resolve(Move move)
    {
        if (!TryResolveCharacterPage(move, out var characterPage))
        {
            return null;
        }

        var pageCode = ResolveCharacterSpecificPageCode(characterPage.CharacterId, move.CanonicalName)
            ?? ResolveGroupedPageCode(move);

        return pageCode is null
            ? BuildCharacterIndexUrl(characterPage)
            : BuildMovePageUrl(characterPage, pageCode);
    }

    private static string? ResolveCharacterSpecificPageCode(string characterId, string moveName)
    {
        if (!CharacterSpecificPageCodes.TryGetValue(characterId, out var pageCodes))
        {
            return null;
        }

        var moveFamily = NormaliseMoveFamily(moveName);
        if (pageCodes.TryGetValue(moveFamily, out var pageCode))
        {
            return pageCode;
        }

        if (moveFamily.StartsWith("ex", StringComparison.OrdinalIgnoreCase)
            && pageCodes.TryGetValue(moveFamily[2..], out var exPageCode))
        {
            return exPageCode;
        }

        return null;
    }

    private static string? ResolveGroupedPageCode(Move move)
    {
        if (!CanUseGroupedPages(move.Section))
        {
            return null;
        }

        var normalised = NormaliseToken(move.CanonicalName);
        if (normalised.Length == 0)
        {
            return null;
        }

        if (normalised is "universaloverhead" or "uoh" or "leapattack")
        {
            return "leap";
        }

        if (normalised is "taunt" or "pa" or "personalaction")
        {
            return "pa";
        }

        if (normalised.Contains("targetcombo", StringComparison.Ordinal)
            || normalised.StartsWith("tc", StringComparison.Ordinal))
        {
            return "tc";
        }

        if (normalised.Contains("throw", StringComparison.Ordinal)
            || normalised.Contains("nage", StringComparison.Ordinal))
        {
            return "nage";
        }

        if (LooksLikeCommandNormal(normalised))
        {
            return "lever";
        }

        if (!TryResolveNormalButton(normalised, out var button, out var isJumping))
        {
            return null;
        }

        var pageCode = ButtonPages[button].PageCode;
        return isJumping ? $"j{pageCode}" : pageCode;
    }

    private static bool CanUseGroupedPages(string section)
    {
        var normalised = NormaliseToken(section);
        return normalised is "normals" or "misc" or "throws";
    }

    private static bool LooksLikeCommandNormal(string normalised)
    {
        if (!ContainsButtonAlias(normalised))
        {
            return false;
        }

        if (normalised.Length >= 3
            && normalised[0] is '1' or '3' or '4' or '6'
            && TryButtonAlias(normalised[1..], out _))
        {
            return true;
        }

        return normalised.StartsWith("toward", StringComparison.Ordinal)
            || normalised.StartsWith("towards", StringComparison.Ordinal)
            || normalised.StartsWith("back", StringComparison.Ordinal)
            || normalised.StartsWith("downback", StringComparison.Ordinal)
            || normalised.StartsWith("downforward", StringComparison.Ordinal)
            || normalised.StartsWith("airdown", StringComparison.Ordinal)
            || normalised.StartsWith("jdown", StringComparison.Ordinal);
    }

    private static bool TryResolveNormalButton(string normalised, out string button, out bool isJumping)
    {
        button = string.Empty;
        isJumping = false;

        if (TryResolveNumpadNormal(normalised, out button, out isJumping))
        {
            return true;
        }

        foreach (var alias in ButtonPages.OrderByDescending(alias => alias.Key.Length))
        {
            if (string.Equals(normalised, alias.Key, StringComparison.Ordinal))
            {
                button = alias.Value.CanonicalButton;
                return true;
            }
        }

        var prefixes = new[]
        {
            ("neutraljumping", true),
            ("verticaljumping", true),
            ("jumping", true),
            ("jump", true),
            ("air", true),
            ("crouching", false),
            ("standing", false),
            ("stand", false),
            ("close", false),
            ("far", false)
        };

        foreach (var (prefix, prefixIsJumping) in prefixes)
        {
            if (!normalised.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var suffix = normalised[prefix.Length..];
            if (!TryButtonAlias(suffix, out button))
            {
                continue;
            }

            isJumping = prefixIsJumping;
            return true;
        }

        return false;
    }

    private static bool TryResolveNumpadNormal(string normalised, out string button, out bool isJumping)
    {
        button = string.Empty;
        isJumping = false;

        if (normalised.Length < 3)
        {
            return false;
        }

        if (normalised[0] is '2' or '5')
        {
            return TryButtonAlias(normalised[1..], out button);
        }

        if (normalised[0] == 'j' && TryButtonAlias(normalised[1..], out button))
        {
            isJumping = true;
            return true;
        }

        return false;
    }

    private static bool TryButtonAlias(string value, out string button)
    {
        button = string.Empty;
        if (ButtonPages.TryGetValue(value, out var resolved))
        {
            button = resolved.CanonicalButton;
            return true;
        }

        return false;
    }

    private static bool ContainsButtonAlias(string normalised)
        => ButtonPages.Keys.Any(alias => normalised.Contains(alias, StringComparison.Ordinal));

    private static bool TryResolveCharacterPage(Move move, out GameRestaurantCharacterPage characterPage)
    {
        var characterKeys = new[]
        {
            move.CharacterId,
            move.CharacterName
        };

        foreach (var key in characterKeys.Select(NormaliseToken))
        {
            if (CharacterPages.TryGetValue(key, out characterPage))
            {
                return true;
            }
        }

        characterPage = default;
        return false;
    }

    private static string BuildCharacterIndexUrl(GameRestaurantCharacterPage characterPage)
        => $"{BaseUrl}/{characterPage.SiteKey}/index.html";

    private static string BuildMovePageUrl(GameRestaurantCharacterPage characterPage, string pageCode)
        => $"{BaseUrl}/{characterPage.SiteKey}/{characterPage.SiteKey}_{pageCode}.html";

    private static string NormaliseMoveFamily(string value)
    {
        var parentheticalIndex = value.IndexOf('(', StringComparison.Ordinal);
        var baseName = parentheticalIndex < 0 ? value : value[..parentheticalIndex];
        return NormaliseToken(baseName);
    }

    private static string NormaliseToken(string value)
        => new(value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());

    private static IReadOnlyDictionary<string, GameRestaurantCharacterPage> CreateCharacterPageMap(
        params GameRestaurantCharacterPage[] pages)
    {
        var map = new Dictionary<string, GameRestaurantCharacterPage>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            map[NormaliseToken(page.CharacterId)] = page;
            map[NormaliseToken(page.SiteKey)] = page;
            foreach (var alias in page.Aliases)
            {
                map[NormaliseToken(alias)] = page;
            }
        }

        return map;
    }

    private static GameRestaurantCharacterPage Character(
        string characterId,
        string? siteKey = null,
        IReadOnlyList<string>? aliases = null)
        => new(characterId, siteKey ?? characterId, aliases ?? []);

    private static IReadOnlyDictionary<string, string> MovePages(params GameRestaurantMovePage[] pages)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            foreach (var moveFamily in page.MoveFamilies)
            {
                map[NormaliseToken(moveFamily)] = page.PageCode;
            }
        }

        return map;
    }

    private static GameRestaurantMovePage Page(string pageCode, params string[] moveFamilies)
        => new(pageCode, moveFamilies);

    private static IReadOnlyDictionary<string, GameRestaurantButtonPage> CreateButtonPageMap(
        params GameRestaurantButtonPage[] buttonPages)
    {
        var map = new Dictionary<string, GameRestaurantButtonPage>(StringComparer.OrdinalIgnoreCase);
        foreach (var buttonPage in buttonPages)
        {
            map[buttonPage.CanonicalButton] = buttonPage;
            foreach (var alias in buttonPage.Aliases)
            {
                map[alias] = buttonPage;
            }
        }

        return map;
    }

    private static GameRestaurantButtonPage Button(
        string canonicalButton,
        string pageCode,
        params string[] aliases)
        => new(canonicalButton, pageCode, aliases);

    private readonly record struct GameRestaurantCharacterPage(
        string CharacterId,
        string SiteKey,
        IReadOnlyList<string> Aliases);

    private readonly record struct GameRestaurantMovePage(
        string PageCode,
        IReadOnlyList<string> MoveFamilies);

    private readonly record struct GameRestaurantButtonPage(
        string CanonicalButton,
        string PageCode,
        IReadOnlyList<string> Aliases);
}
