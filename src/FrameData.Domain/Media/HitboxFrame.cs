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
        var normalised = Normalise(type);
        return normalised.StartsWith("P2_", StringComparison.Ordinal)
            || normalised.StartsWith("P2", StringComparison.Ordinal)
            || normalised.Contains("_P2_", StringComparison.Ordinal);
    }

    public static bool IsActiveAreaHitbox(string type)
    {
        var normalised = Normalise(type);
        if (IsP2(normalised))
        {
            return false;
        }

        return normalised == "P1_A"
            || normalised == "OBJ_A"
            || normalised == "OBJECT_A"
            || normalised == "PROJECTILE_A"
            || normalised == "P1_OA"
            || normalised == "P1_OBJECT_A"
            || normalised == "P1_PROJECTILE_A"
            || (normalised.StartsWith("P1_", StringComparison.Ordinal) && normalised.EndsWith("_A", StringComparison.Ordinal));
    }

    public static bool IsActiveThrowHitbox(string type)
    {
        var normalised = Normalise(type);
        if (IsP2(normalised))
        {
            return false;
        }

        return normalised == "P1_T"
            || normalised == "OBJ_T"
            || normalised == "OBJECT_T"
            || normalised == "P1_OBJECT_T"
            || (normalised.StartsWith("P1_", StringComparison.Ordinal) && normalised.EndsWith("_T", StringComparison.Ordinal));
    }

    public static bool ShouldRender(string type, IReadOnlyCollection<string> overlays)
    {
        if (IsP2(type))
        {
            return false;
        }

        var normalised = Normalise(type);
        return overlays.Any(overlay => string.Equals(Normalise(overlay), normalised, StringComparison.Ordinal))
            || IsActiveAreaHitbox(normalised)
            || IsActiveThrowHitbox(normalised);
    }

    public static string Normalise(string value)
        => value.Trim()
            .Replace('-', '_')
            .Replace(' ', '_')
            .ToUpperInvariant();
}
