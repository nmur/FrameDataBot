namespace FrameData.Domain.Media;

public sealed class HitboxFrame
{
    public required string FrameId { get; init; }
    public string? SourceFrameImageUrl { get; init; }
    public IReadOnlyList<HitboxRectangle> Hitboxes { get; init; } = [];
}

public sealed class HitboxRectangle
{
    public required string Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }

    public int Area => Math.Max(0, Width) * Math.Max(0, Height);
}

public static class HitboxOverlayTypes
{
    public static readonly IReadOnlyList<string> DefaultP1Overlays =
    [
        "P1_P",
        "P1_V",
        "P1_A",
        "P1_T",
        "P1_TA"
    ];

    public static bool IsP2(string type)
    {
        var normalized = Normalize(type);
        return normalized.StartsWith("P2_", StringComparison.Ordinal)
            || normalized.StartsWith("P2", StringComparison.Ordinal)
            || normalized.Contains("_P2_", StringComparison.Ordinal);
    }

    public static bool IsActiveAreaHitbox(string type)
    {
        var normalized = Normalize(type);
        if (IsP2(normalized))
        {
            return false;
        }

        return normalized == "P1_A"
            || normalized == "OBJ_A"
            || normalized == "OBJECT_A"
            || normalized == "PROJECTILE_A"
            || normalized == "P1_OA"
            || normalized == "P1_OBJECT_A"
            || normalized == "P1_PROJECTILE_A"
            || (normalized.StartsWith("P1_", StringComparison.Ordinal) && normalized.EndsWith("_A", StringComparison.Ordinal));
    }

    public static bool ShouldRender(string type, IReadOnlyCollection<string> overlays)
    {
        if (IsP2(type))
        {
            return false;
        }

        var normalized = Normalize(type);
        return overlays.Any(overlay => string.Equals(Normalize(overlay), normalized, StringComparison.Ordinal))
            || IsActiveAreaHitbox(normalized);
    }

    public static string Normalize(string value)
        => value.Trim()
            .Replace('-', '_')
            .Replace(' ', '_')
            .ToUpperInvariant();
}
