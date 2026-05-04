using AngleSharp.Html.Parser;

namespace FrameData.Scraper.Parsing;

public sealed class CharacterSectionParser
{
    private static readonly HashSet<string> SupportedSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "Normals",
        "Specials",
        "Super Arts",
        "Misc"
    };

    public IReadOnlyList<ParsedMoveEntry> Parse(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var parsed = new List<ParsedMoveEntry>();

        var sectionHeadings = document.QuerySelectorAll("h1,h2,h3,h4");
        foreach (var heading in sectionHeadings)
        {
            var section = heading.TextContent.Trim();
            if (!SupportedSections.Contains(section))
            {
                continue;
            }

            var table = FindNextTable(heading);
            if (table is null)
            {
                continue;
            }

            parsed.AddRange(ParseTable(section, table));
        }

        return parsed;
    }

    private static IEnumerable<ParsedMoveEntry> ParseTable(string section, AngleSharp.Dom.IElement table)
    {
        var rows = table.QuerySelectorAll("tr");
        if (rows.Length < 2)
        {
            yield break;
        }

        var headers = rows[0].QuerySelectorAll("th,td")
            .Select(c => NormaliseHeader(c.TextContent))
            .ToArray();

        var moveIndex = FindFirstHeaderIndex(headers, "move", "name");
        if (moveIndex < 0)
        {
            yield break;
        }

        var startupIndex = FindHeaderIndex(headers, "startup");
        var activeIndex = FindFirstHeaderIndex(headers, "active", "hit");
        var recoveryIndex = FindHeaderIndex(headers, "recovery");
        var onHitIndex = FindFirstHeaderIndex(headers, "onhit", "hitadv");
        var onBlockIndex = FindFirstHeaderIndex(headers, "onblock", "blockadv", "blkadv");
        var frameAdvantageIndex = FindHeaderIndex(headers, "frameadvantage");
        var motionIndex = FindHeaderIndex(headers, "motion");
        var damageIndex = FindFirstHeaderIndex(headers, "damage", "dmg");
        var stunIndex = FindHeaderIndex(headers, "stun");
        if (frameAdvantageIndex < 0)
        {
            frameAdvantageIndex = onBlockIndex;
        }

        foreach (var row in rows.Skip(1))
        {
            var cells = row.QuerySelectorAll("td");
            if (cells.Length == 0 || moveIndex >= cells.Length)
            {
                continue;
            }

            var moveName = cells[moveIndex].TextContent.Trim();
            if (string.IsNullOrWhiteSpace(moveName))
            {
                continue;
            }

            var hitboxDisplayPath = FindHitboxDisplayPath(row, cells[moveIndex]);
            var sourceMoveId = ReadQueryParameter(hitboxDisplayPath, "iMove")
                ?? FindSourceMoveId(row);
            yield return new ParsedMoveEntry
            {
                Section = section,
                CanonicalName = moveName,
                SourceMoveId = sourceMoveId,
                SourceHitboxPath = hitboxDisplayPath,
                Startup = GetCellValue(cells, startupIndex),
                Active = GetCellValue(cells, activeIndex),
                Recovery = GetCellValue(cells, recoveryIndex),
                OnHit = GetCellValue(cells, onHitIndex),
                OnBlock = GetCellValue(cells, onBlockIndex),
                FrameAdvantage = GetCellValue(cells, frameAdvantageIndex),
                Motion = GetCellValue(cells, motionIndex),
                Damage = GetCellValue(cells, damageIndex),
                Stun = GetCellValue(cells, stunIndex)
            };
        }
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, string normalisedTarget)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i] == normalisedTarget)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindFirstHeaderIndex(IReadOnlyList<string> headers, params string[] normalisedTargets)
    {
        foreach (var target in normalisedTargets)
        {
            var index = FindHeaderIndex(headers, target);
            if (index >= 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static string NormaliseHeader(string value)
        => new string(value.Where(char.IsLetterOrDigit).ToArray())
            .ToLowerInvariant();

    private static string? GetCellValue(AngleSharp.Dom.IHtmlCollection<AngleSharp.Dom.IElement> cells, int index)
    {
        if (index < 0 || index >= cells.Length)
        {
            return null;
        }

        var value = cells[index].TextContent.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FindHitboxDisplayPath(AngleSharp.Dom.IElement row, AngleSharp.Dom.IElement moveCell)
    {
        var link = row.QuerySelector("a[href*='hitboxesDisplay.php']")
            ?? moveCell.QuerySelector("a[href]");

        var href = link?.GetAttribute("href")?.Trim();
        return string.IsNullOrWhiteSpace(href) ? null : href;
    }

    private static string? FindSourceMoveId(AngleSharp.Dom.IElement row)
    {
        var hitboxLink = row.QuerySelector(".linkHitboxes");
        var linkId = hitboxLink?.GetAttribute("id");
        if (!string.IsNullOrWhiteSpace(linkId) && linkId.StartsWith("load_", StringComparison.OrdinalIgnoreCase))
        {
            return linkId["load_".Length..].Trim();
        }

        var firstCell = row.QuerySelector("td");
        var title = firstCell?.GetAttribute("title");
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        var hiddenValue = hitboxLink?.QuerySelector(".none")?.TextContent.Trim();
        return string.IsNullOrWhiteSpace(hiddenValue) ? null : hiddenValue;
    }

    private static string? ReadQueryParameter(string? url, string name)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var questionMarkIndex = url.IndexOf('?', StringComparison.Ordinal);
        if (questionMarkIndex < 0 || questionMarkIndex == url.Length - 1)
        {
            return null;
        }

        var query = url[(questionMarkIndex + 1)..];
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var equalsIndex = pair.IndexOf('=', StringComparison.Ordinal);
            var key = equalsIndex < 0 ? pair : pair[..equalsIndex];
            if (!string.Equals(Uri.UnescapeDataString(key), name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = equalsIndex < 0 ? string.Empty : pair[(equalsIndex + 1)..];
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }

        return null;
    }

    private static AngleSharp.Dom.IElement? FindNextTable(AngleSharp.Dom.IElement heading)
    {
        var current = heading.NextElementSibling;
        while (current is not null)
        {
            if (string.Equals(current.TagName, "TABLE", StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            var nestedTable = current.QuerySelector("table");
            if (nestedTable is not null)
            {
                return nestedTable;
            }

            current = current.NextElementSibling;
        }

        return null;
    }
}

public sealed class ParsedMoveEntry
{
    public required string Section { get; init; }
    public required string CanonicalName { get; init; }
    public string? SourceMoveId { get; init; }
    public string? SourceHitboxPath { get; init; }
    public string? Startup { get; init; }
    public string? Active { get; init; }
    public string? Recovery { get; init; }
    public string? OnHit { get; init; }
    public string? OnBlock { get; init; }
    public string? FrameAdvantage { get; init; }
    public string? Motion { get; init; }
    public string? Damage { get; init; }
    public string? Stun { get; init; }
}
