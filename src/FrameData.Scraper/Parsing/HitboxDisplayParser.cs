using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FrameData.Domain.Media;

namespace FrameData.Scraper.Parsing;

public sealed partial class HitboxDisplayParser
{
    public IReadOnlyList<HitboxFrame> Parse(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var jsonFrames = ParseJsonFrames(document).ToArray();
        if (jsonFrames.Length > 0)
        {
            return jsonFrames;
        }

        var dataFrameElements = document.QuerySelectorAll("[data-frame], [data-frame-id]")
            .Where(element => !string.IsNullOrWhiteSpace(ReadAttribute(element, "data-frame", "data-frame-id")))
            .ToArray();

        if (dataFrameElements.Length > 0)
        {
            return dataFrameElements
                .Select(ParseFrameElement)
                .Where(frame => frame.Hitboxes.Count > 0 || !string.IsNullOrWhiteSpace(frame.SourceFrameImageUrl))
                .OrderBy(frame => FrameSortKey(frame.FrameId))
                .ThenBy(frame => frame.FrameId, StringComparer.Ordinal)
                .ToArray();
        }

        return ParseTableRows(document)
            .OrderBy(frame => FrameSortKey(frame.FrameId))
            .ThenBy(frame => frame.FrameId, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<HitboxFrame> ParseJsonFrames(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script[type='application/json'], script[data-hitbox-frames]"))
        {
            var text = script.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foreach (var frame in ParseJsonFramePayload(text))
            {
                yield return frame;
            }
        }
    }

    private static IEnumerable<HitboxFrame> ParseJsonFramePayload(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var framesElement = root.ValueKind == JsonValueKind.Array
            ? root
            : TryGetProperty(root, "frames", out var nestedFrames) ? nestedFrames : default;

        if (framesElement.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var frameElement in framesElement.EnumerateArray())
        {
            var frameId = ReadString(frameElement, "frame", "frameId", "id");
            if (string.IsNullOrWhiteSpace(frameId))
            {
                continue;
            }

            var hitboxes = new List<HitboxRectangle>();
            if (TryGetProperty(frameElement, "hitboxes", out var hitboxesElement) && hitboxesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var hitboxElement in hitboxesElement.EnumerateArray())
                {
                    var type = ReadString(hitboxElement, "type", "boxType", "name");
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    hitboxes.Add(new HitboxRectangle
                    {
                        Type = type,
                        X = ReadInt(hitboxElement, "x", "left"),
                        Y = ReadInt(hitboxElement, "y", "top"),
                        Width = ReadInt(hitboxElement, "width", "w"),
                        Height = ReadInt(hitboxElement, "height", "h")
                    });
                }
            }

            yield return new HitboxFrame
            {
                FrameId = frameId,
                SourceFrameImageUrl = ReadString(frameElement, "imageUrl", "sourceFrameImageUrl", "src"),
                Hitboxes = hitboxes
            };
        }
    }

    private static HitboxFrame ParseFrameElement(IElement frameElement)
    {
        var frameId = ReadAttribute(frameElement, "data-frame", "data-frame-id")!;
        var imageUrl = ReadAttribute(frameElement, "data-frame-image-url", "data-image-url")
            ?? frameElement.QuerySelector("img")?.GetAttribute("src");

        var hitboxes = frameElement
            .QuerySelectorAll("[data-hitbox-type], [data-box-type], [data-type], .P1_P, .P1_V, .P1_A, .P1_T, .P1_TA, .P2_P, .P2_V, .P2_A, .P2_T, .P2_TA")
            .Select(ParseHitboxElement)
            .Where(hitbox => hitbox is not null)
            .Select(hitbox => hitbox!)
            .ToArray();

        return new HitboxFrame
        {
            FrameId = frameId,
            SourceFrameImageUrl = imageUrl,
            Hitboxes = hitboxes
        };
    }

    private static HitboxRectangle? ParseHitboxElement(IElement element)
    {
        var type = ReadAttribute(element, "data-hitbox-type", "data-box-type", "data-type")
            ?? ReadTypeFromClass(element);

        if (string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        if (!TryReadRectangle(element, out var x, out var y, out var width, out var height))
        {
            return null;
        }

        return new HitboxRectangle
        {
            Type = type,
            X = x,
            Y = y,
            Width = width,
            Height = height
        };
    }

    private static IReadOnlyList<HitboxFrame> ParseTableRows(IDocument document)
    {
        var rows = document.QuerySelectorAll("tr[data-frame], tr[data-frame-id]");
        var frames = new Dictionary<string, List<HitboxRectangle>>(StringComparer.OrdinalIgnoreCase);
        var imageUrls = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var frameId = ReadAttribute(row, "data-frame", "data-frame-id");
            if (string.IsNullOrWhiteSpace(frameId))
            {
                continue;
            }

            var type = ReadAttribute(row, "data-hitbox-type", "data-box-type", "data-type");
            if (string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            if (!frames.TryGetValue(frameId, out var hitboxes))
            {
                hitboxes = [];
                frames.Add(frameId, hitboxes);
                imageUrls.Add(frameId, ReadAttribute(row, "data-frame-image-url", "data-image-url"));
            }

            hitboxes.Add(new HitboxRectangle
            {
                Type = type,
                X = ReadIntAttribute(row, "data-x", "data-left"),
                Y = ReadIntAttribute(row, "data-y", "data-top"),
                Width = ReadIntAttribute(row, "data-width", "data-w"),
                Height = ReadIntAttribute(row, "data-height", "data-h")
            });
        }

        return frames
            .Select(pair => new HitboxFrame
            {
                FrameId = pair.Key,
                SourceFrameImageUrl = imageUrls[pair.Key],
                Hitboxes = pair.Value
            })
            .ToArray();
    }

    private static bool TryReadRectangle(IElement element, out int x, out int y, out int width, out int height)
    {
        x = ReadIntAttribute(element, "data-x", "data-left");
        y = ReadIntAttribute(element, "data-y", "data-top");
        width = ReadIntAttribute(element, "data-width", "data-w");
        height = ReadIntAttribute(element, "data-height", "data-h");

        var style = element.GetAttribute("style");
        if (!string.IsNullOrWhiteSpace(style))
        {
            x = ReadStyleInt(style, "left") ?? x;
            y = ReadStyleInt(style, "top") ?? y;
            width = ReadStyleInt(style, "width") ?? width;
            height = ReadStyleInt(style, "height") ?? height;
        }

        return width > 0 && height > 0;
    }

    private static string? ReadTypeFromClass(IElement element)
        => element.ClassList
            .Select(candidate => candidate.Trim())
            .FirstOrDefault(candidate => KnownBoxTypeRegex().IsMatch(candidate));

    private static string? ReadAttribute(IElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = element.GetAttribute(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static int ReadIntAttribute(IElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (int.TryParse(element.GetAttribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static int? ReadStyleInt(string style, string propertyName)
    {
        var match = Regex.Match(
            style,
            $@"(?:^|;)\s*{Regex.Escape(propertyName)}\s*:\s*(?<value>-?\d+)(?:px)?",
            RegexOptions.IgnoreCase);

        return match.Success
            ? int.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture)
            : null;
    }

    private static string? ReadString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (TryGetProperty(element, propertyName, out var property)
                && property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }
        }

        return null;
    }

    private static int ReadInt(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(element, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var parsedNumber))
            {
                return parsedNumber;
            }

            if (property.ValueKind == JsonValueKind.String
                && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedString))
            {
                return parsedString;
            }
        }

        return 0;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static int FrameSortKey(string frameId)
        => int.TryParse(frameId, out var parsed) ? parsed : int.MaxValue;

    [GeneratedRegex(@"^(?:P[12]_(?:P|V|A|T|TA)|OBJ_A|OBJECT_A|PROJECTILE_A|P1_(?:OA|OBJECT_A|PROJECTILE_A))$", RegexOptions.IgnoreCase)]
    private static partial Regex KnownBoxTypeRegex();
}
