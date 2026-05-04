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
            ["alex"] = MovePages(
                Page("lever", "towards strong", "towards fierce", "back fierce", "(air) down fierce", "down fierce air"),
                Page("h1", "flash chop"),
                Page("h2", "air knee smash"),
                Page("h3", "air stampede"),
                Page("h4", "slash elbow"),
                Page("h5", "power bomb", "reverse power bomb"),
                Page("h6", "spiral ddt"),
                Page("sa1", "hyper bomb"),
                Page("sa2", "boomerang raid"),
                Page("sa3", "stun gun headbutt")),
            ["akuma"] = MovePages(
                Page("lever", "towards strong", "dive kick"),
                Page("h1", "gou hadouken"),
                Page("h2", "zankuu hadouken", "tenma gou zankuu"),
                Page("h3", "shakunetsu hadouken"),
                Page("h4", "gou shoryuken"),
                Page("h5", "tatsumaki zankuu kyaku"),
                Page("h6", "air tatsumaki zankuu kyaku"),
                Page("h7", "hyakki shuu", "hyakki goushou", "hyakki goujin", "hyakki gousai"),
                Page("h8", "ashura senkuu"),
                Page("sa1", "messatsu gou hadou"),
                Page("sa2", "messatsu gou shoryuu"),
                Page("sa3", "messatsu gou rasen"),
                Page("sa4", "shungokusatsu"),
                Page("sa5", "kongou kokuretsu zan")),
            ["chun-li"] = MovePages(
                Page("lever", "back strong", "back fierce", "towards forward", "down towards roundhouse", "(air) down forward"),
                Page("h1", "hyakuretsu kyaku"),
                Page("h2", "kikoken", "kikouken", "kikkoken"),
                Page("h3", "spinning bird kick"),
                Page("h4", "hazan shu"),
                Page("sa1", "kikoshou"),
                Page("sa2", "houyokusen"),
                Page("sa3", "tensei ranka")),
            ["dudley"] = MovePages(
                Page("lever", "towards strong", "towards roundhouse", "towards forward", "towards fierce", "towards jab"),
                Page("h1", "jet uppercut"),
                Page("h2", "ducking rush", "ducking straight", "ducking upper"),
                Page("h3", "machine gun blow"),
                Page("h4", "cross counter"),
                Page("h5", "short swing blow"),
                Page("sa1", "rocket uppercut"),
                Page("sa2", "rolling thunder"),
                Page("sa3", "corkscrew blow")),
            ["elena"] = MovePages(
                Page("lever", "towards strong", "down towards roundhouse", "towards forward", "back roundhouse"),
                Page("h1", "scratch wheel"),
                Page("h2", "rhino horn"),
                Page("h3", "mallet smash"),
                Page("h4", "spinning scythe"),
                Page("h5", "lynx tail"),
                Page("sa1", "spinning beat"),
                Page("sa2", "brave dance"),
                Page("sa3", "healing")),
            ["hugo"] = MovePages(
                Page("lever", "towards fierce", "down fierce air", "(air) down fierce"),
                Page("h1", "moonsault press"),
                Page("h2", "ultra throw"),
                Page("h3", "shootdown backbreaker"),
                Page("h4", "meat squasher"),
                Page("h5", "giant palm bomber"),
                Page("h6", "monster lariat"),
                Page("sa1", "gigas breaker"),
                Page("sa2", "megaton press"),
                Page("sa3", "hammer frenzy", "hammer mountain")),
            ["ibuki"] = MovePages(
                Page("lever", "back strong", "towards short", "towards forward", "back forward", "down towards forward", "towards roundhouse"),
                Page("h1", "kunai"),
                Page("h2", "kubiori"),
                Page("h3", "kazekiri"),
                Page("h4", "tsumuji"),
                Page("h5", "hien"),
                Page("h6", "raida"),
                Page("h8", "kasumi gake"),
                Page("sa1", "kasumi suzaku"),
                Page("sa2", "yoroi doushi", "missed grab"),
                Page("sa3", "yami shigure")),
            ["ken"] = MovePages(
                Page("lever", "towards forward", "back forward", "towards roundhouse"),
                Page("mk", "hold forward"),
                Page("h1", "hadouken"),
                Page("h2", "shoryuken"),
                Page("h3", "tatsumaki senpuu kyaku"),
                Page("h4", "air tatsumaki senpuu kyaku"),
                Page("sa1", "shoryu reppa"),
                Page("sa2", "shinryuken"),
                Page("sa3", "shipuu jinrai kyaku")),
            ["makoto"] = MovePages(
                Page("lever", "towards jab", "towards strong", "towards fierce", "towards fierce complete", "towards short", "towards forward", "towards roundhouse"),
                Page("h1", "hayate"),
                Page("h2", "fukiage"),
                Page("h3", "oroshi"),
                Page("h4", "karakusa"),
                Page("h5", "tsurugi"),
                Page("sa1", "seichuusen godanzuki"),
                Page("sa2", "abare tosanami"),
                Page("sa3", "tanden renki")),
            ["necro"] = MovePages(
                Page("lever", "1hp", "downbackhp", "downbackfierce", "down back fierce", "drill short", "drill forward", "drill rh"),
                Page("h1", "electric shock"),
                Page("h2", "spinning punch"),
                Page("h3", "flying viper"),
                Page("h4", "raging cobra"),
                Page("h5", "snake fang"),
                Page("sa1", "magnetic storm"),
                Page("sa2", "slam dance"),
                Page("sa3", "electric snake")),
            ["oro"] = MovePages(
                Page("lever", "towards strong"),
                Page("h1", "sun disk palm"),
                Page("h2", "oniyama"),
                Page("h3", "human pillar driver"),
                Page("h4", "jinchu nobori", "air jinchu nobori"),
                Page("sa1", "kishin riki", "ground grab", "air grab", "ex kishin riki"),
                Page("sa2", "yagyou dama", "ex yagyou dama"),
                Page("sa3", "tengu stones")),
            ["q"] = MovePages(
                Page("lever", "back strong", "back fierce", "back roundhouse"),
                Page("h1", "dashing head attack"),
                Page("h2", "dashing middle attack"),
                Page("h3", "dashing leg attack"),
                Page("h4", "high speed barrage"),
                Page("h5", "capture and deadly blow"),
                Page("sa1", "critical combo attack"),
                Page("sa2", "deadly double combination"),
                Page("sa3", "total destruction", "far grab", "close grab")),
            ["remy"] = MovePages(
                Page("lever", "towards forward"),
                Page("h1", "light of virtue jab", "light of virtue strong", "light of virtue fierce", "light of virtue ex high"),
                Page("h2", "light of virtue short", "light of virtue forward", "light of virtue rh", "light of virtue ex low"),
                Page("h3", "rising rage flash"),
                Page("h4", "cold blue kick"),
                Page("sa1", "light of justice"),
                Page("sa2", "supreme rising rage flash"),
                Page("sa3", "blue nocturne")),
            ["ryu"] = MovePages(
                Page("lever", "towards strong", "towards fierce"),
                Page("h1", "hadouken"),
                Page("h2", "shoryuken"),
                Page("h3", "tatsumakisenpuukyaku"),
                Page("h4", "airtatsumakisenpuukyaku"),
                Page("h5", "joudansokutougeri"),
                Page("sa1", "shinkuuhadouken"),
                Page("sa1", "shinkuhadouken"),
                Page("sa2", "shinshoryuken"),
                Page("sa3", "denjinhadouken")),
            ["sean"] = MovePages(
                Page("lever", "towards fierce", "towards roundhouse"),
                Page("h1", "sean roll"),
                Page("h2", "dragon smash"),
                Page("h3", "tornado kick"),
                Page("h4", "ryuubi kyaku"),
                Page("h5", "tackle"),
                Page("sa1", "hadou burst"),
                Page("sa2", "shoryuu cannon"),
                Page("sa3", "hyper tornado")),
            ["twelve"] = MovePages(
                Page("lever", "back forward"),
                Page("h1", "ndl"),
                Page("h2", "axe"),
                Page("h3", "air axe"),
                Page("h4", "dra"),
                Page("sa1", "xndl"),
                Page("sa2", "xflat"),
                Page("sa3", "xcopy")),
            ["urien"] = MovePages(
                Page("lever", "towards strong", "towards fierce", "towards forward"),
                Page("h1", "metallic sphere"),
                Page("h2", "chariot rush"),
                Page("h3", "violence knee drop"),
                Page("h4", "headbutt"),
                Page("sa1", "tyrant slaughter", "tyrant punish"),
                Page("sa2", "temporal thunder"),
                Page("sa3", "aegis reflector")),
            ["yang"] = MovePages(
                Page("lever", "towards forward", "dive kicks"),
                Page("h1", "tourou zan"),
                Page("h2", "senkyuutai"),
                Page("h3", "kaihou"),
                Page("h4", "byakko soushouda"),
                Page("h5", "zenpou tenshin"),
                Page("sa1", "raishin mahhaken"),
                Page("sa2", "tenshin senkyuutai"),
                Page("sa3", "seiei enbu")),
            ["yun"] = MovePages(
                Page("lever", "towards forward", "towards fierce", "dive kicks"),
                Page("h1", "zesshou hohou"),
                Page("h2", "tetsu zankou"),
                Page("h3", "nishou kyaku"),
                Page("h4", "kobokushi"),
                Page("h5", "zenpou tenshin"),
                Page("sa1", "youhou"),
                Page("sa2", "sourai rengeki"),
                Page("sa3", "genei jin"))
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

        var fullMoveName = NormaliseToken(moveName);
        if (pageCodes.TryGetValue(fullMoveName, out var pageCode))
        {
            return pageCode;
        }

        var moveFamily = NormaliseMoveFamily(moveName);
        if (pageCodes.TryGetValue(moveFamily, out pageCode))
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
            ("far", false),
            ("downtowards", false),
            ("downback", false),
            ("towards", false),
            ("back", false),
            ("hold", false),
            ("down", false)
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

        if (normalised[0] is '1' or '2' or '3' or '4' or '5' or '6')
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
