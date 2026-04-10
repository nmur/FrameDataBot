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
            .Select(c => NormalizeHeader(c.TextContent))
            .ToArray();

        var moveIndex = FindHeaderIndex(headers, "move");
        if (moveIndex < 0)
        {
            yield break;
        }

        var startupIndex = FindHeaderIndex(headers, "startup");
        var activeIndex = FindHeaderIndex(headers, "active");
        var recoveryIndex = FindHeaderIndex(headers, "recovery");
        var onHitIndex = FindHeaderIndex(headers, "onhit");
        var onBlockIndex = FindHeaderIndex(headers, "onblock");
        var frameAdvantageIndex = FindHeaderIndex(headers, "frameadvantage");

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

            yield return new ParsedMoveEntry
            {
                Section = section,
                CanonicalName = moveName,
                Startup = GetCellValue(cells, startupIndex),
                Active = GetCellValue(cells, activeIndex),
                Recovery = GetCellValue(cells, recoveryIndex),
                OnHit = GetCellValue(cells, onHitIndex),
                OnBlock = GetCellValue(cells, onBlockIndex),
                FrameAdvantage = GetCellValue(cells, frameAdvantageIndex)
            };
        }
    }

    private static int FindHeaderIndex(IReadOnlyList<string> headers, string normalizedTarget)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (headers[i] == normalizedTarget)
            {
                return i;
            }
        }

        return -1;
    }

    private static string NormalizeHeader(string value)
        => new string(value.Where(ch => !char.IsWhiteSpace(ch) && ch != '-' && ch != '_').ToArray())
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

    private static AngleSharp.Dom.IElement? FindNextTable(AngleSharp.Dom.IElement heading)
    {
        var current = heading.NextElementSibling;
        while (current is not null)
        {
            if (string.Equals(current.TagName, "TABLE", StringComparison.OrdinalIgnoreCase))
            {
                return current;
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
    public string? Startup { get; init; }
    public string? Active { get; init; }
    public string? Recovery { get; init; }
    public string? OnHit { get; init; }
    public string? OnBlock { get; init; }
    public string? FrameAdvantage { get; init; }
}
