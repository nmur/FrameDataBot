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

        var sourceScriptFrames = ParseSourceScriptFrames(document).ToArray();
        if (sourceScriptFrames.Length > 0)
        {
            return sourceScriptFrames;
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

    private static IEnumerable<HitboxFrame> ParseSourceScriptFrames(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script"))
        {
            var text = script.TextContent;
            if (string.IsNullOrWhiteSpace(text) || !text.Contains("aFramesInfos", StringComparison.Ordinal))
            {
                continue;
            }

            var framesJson = ExtractObjectLiteral(text, "aFramesInfos");
            if (string.IsNullOrWhiteSpace(framesJson))
            {
                continue;
            }

            foreach (var frame in ParseSourceFramePayload(framesJson, ReadJavaScriptStringVariable(text, "sBaseUrl")))
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
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var frame in ParseFrameObjectProperties(root, sourceFrameBaseUrl: null))
                {
                    yield return frame;
                }
            }

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

    private static IEnumerable<HitboxFrame> ParseSourceFramePayload(string json, string? sourceFrameBaseUrl)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (var frame in ParseFrameObjectProperties(document.RootElement, sourceFrameBaseUrl))
        {
            yield return frame;
        }
    }

    private static IEnumerable<HitboxFrame> ParseFrameObjectProperties(JsonElement framesObject, string? sourceFrameBaseUrl)
    {
        foreach (var frameProperty in framesObject.EnumerateObject())
        {
            var frameElement = frameProperty.Value;
            if (frameElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var frameId = ReadString(frameElement, "frame", "frameId", "id") ?? frameProperty.Name;
            var hitboxes = new List<HitboxRectangle>();
            AddSourceDrawBoxes(frameElement, "P1", "p_hb_to_draw", "P1_P", hitboxes);
            AddSourceDrawBoxes(frameElement, "P1", "v_hb_to_draw", "P1_V", hitboxes);
            AddSourceDrawBoxes(frameElement, "P1", "a_hb_to_draw", "P1_A", hitboxes);
            AddSourceDrawBoxes(frameElement, "P1", "t_hb_to_draw", "P1_T", hitboxes);
            AddSourceDrawBoxes(frameElement, "P1", "ta_hb_to_draw", "P1_TA", hitboxes);
            AddSourceDrawBoxes(frameElement, "P2", "p_hb_to_draw", "P2_P", hitboxes);
            AddSourceDrawBoxes(frameElement, "P2", "v_hb_to_draw", "P2_V", hitboxes);
            AddSourceDrawBoxes(frameElement, "P2", "a_hb_to_draw", "P2_A", hitboxes);
            AddSourceDrawBoxes(frameElement, "P2", "t_hb_to_draw", "P2_T", hitboxes);
            AddSourceDrawBoxes(frameElement, "P2", "ta_hb_to_draw", "P2_TA", hitboxes);
            AddSourceObjectDrawBoxes(frameElement, hitboxes);

            yield return new HitboxFrame
            {
                FrameId = frameId,
                SourceFrameImageUrl = BuildSourceFrameImageUrl(sourceFrameBaseUrl, ReadString(frameElement, "pngFileName")),
                Hitboxes = hitboxes
            };
        }
    }

    private static void AddSourceObjectDrawBoxes(JsonElement frameElement, ICollection<HitboxRectangle> hitboxes)
    {
        foreach (var actorKey in GetSourceObjectActorKeys(frameElement))
        {
            AddSourceDrawBoxes(frameElement, actorKey, "a_hb_to_draw", "OBJECT_A", hitboxes);
        }
    }

    private static IEnumerable<string> GetSourceObjectActorKeys(JsonElement frameElement)
    {
        var actorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (TryGetProperty(frameElement, "objects_list", out var objectsElement)
            && objectsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var objectElement in objectsElement.EnumerateArray())
            {
                if (objectElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(objectElement.GetString()))
                {
                    actorKeys.Add(objectElement.GetString()!.Trim());
                }
            }
        }

        foreach (var property in frameElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object
                && property.Name.StartsWith("OBJECT_", StringComparison.OrdinalIgnoreCase))
            {
                actorKeys.Add(property.Name);
            }
        }

        return actorKeys;
    }

    private static void AddSourceDrawBoxes(
        JsonElement frameElement,
        string actorKey,
        string drawBoxesKey,
        string hitboxType,
        ICollection<HitboxRectangle> hitboxes)
    {
        if (!TryGetProperty(frameElement, actorKey, out var actorElement)
            || actorElement.ValueKind != JsonValueKind.Object
            || !TryGetProperty(actorElement, "hitboxes", out var hitboxesElement)
            || hitboxesElement.ValueKind != JsonValueKind.Object
            || !TryGetProperty(hitboxesElement, drawBoxesKey, out var drawBoxesElement)
            || drawBoxesElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var drawBox in drawBoxesElement.EnumerateArray())
        {
            if (drawBox.ValueKind != JsonValueKind.Array || drawBox.GetArrayLength() < 4)
            {
                continue;
            }

            var x1 = ReadArrayInt(drawBox, 0);
            var x2 = ReadArrayInt(drawBox, 1);
            var y1 = ReadArrayInt(drawBox, 2);
            var y2 = ReadArrayInt(drawBox, 3);
            var width = Math.Abs(x2 - x1);
            var height = Math.Abs(y2 - y1);
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            hitboxes.Add(new HitboxRectangle
            {
                Type = hitboxType,
                X = Math.Min(x1, x2),
                Y = Math.Min(y1, y2),
                Width = width,
                Height = height
            });
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

    private static int ReadArrayInt(JsonElement array, int index)
    {
        var value = array[index];
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsedNumber))
        {
            return parsedNumber;
        }

        if (value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedString))
        {
            return parsedString;
        }

        return 0;
    }

    private static string? BuildSourceFrameImageUrl(string? sourceFrameBaseUrl, string? pngFileName)
    {
        if (string.IsNullOrWhiteSpace(pngFileName))
        {
            return null;
        }

        var trimmedFileName = pngFileName.Trim();
        if (Uri.TryCreate(trimmedFileName, UriKind.Absolute, out var absoluteFrameUrl))
        {
            return absoluteFrameUrl.ToString();
        }

        if (string.IsNullOrWhiteSpace(sourceFrameBaseUrl))
        {
            return trimmedFileName;
        }

        var trimmedBaseUrl = sourceFrameBaseUrl.Trim();
        if (Uri.TryCreate(trimmedBaseUrl, UriKind.Absolute, out var absoluteBaseUrl)
            && Uri.TryCreate(absoluteBaseUrl, trimmedFileName, out var resolvedFrameUrl))
        {
            return resolvedFrameUrl.ToString();
        }

        return $"{trimmedBaseUrl.TrimEnd('/')}/{trimmedFileName.TrimStart('/')}";
    }

    private static string? ReadJavaScriptStringVariable(string script, string variableName)
    {
        var match = Regex.Match(
            script,
            $@"(?:\bvar\s+|\b){Regex.Escape(variableName)}\s*=\s*(?<quote>['""])(?<value>.*?)(\k<quote>)",
            RegexOptions.Singleline);

        return match.Success ? match.Groups["value"].Value : null;
    }

    private static string? ExtractObjectLiteral(string script, string variableName)
    {
        var variableIndex = script.IndexOf(variableName, StringComparison.Ordinal);
        if (variableIndex < 0)
        {
            return null;
        }

        var assignmentIndex = script.IndexOf('=', variableIndex + variableName.Length);
        if (assignmentIndex < 0)
        {
            return null;
        }

        var objectStartIndex = script.IndexOf('{', assignmentIndex + 1);
        if (objectStartIndex < 0)
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;
        var stringQuote = '\0';

        for (var index = objectStartIndex; index < script.Length; index++)
        {
            var ch = script[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (ch == stringQuote)
                {
                    inString = false;
                }

                continue;
            }

            if (ch is '"' or '\'')
            {
                inString = true;
                stringQuote = ch;
                continue;
            }

            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return script[objectStartIndex..(index + 1)];
                }
            }
        }

        return null;
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
