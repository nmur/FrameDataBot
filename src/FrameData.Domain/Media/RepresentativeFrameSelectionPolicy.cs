namespace FrameData.Domain.Media;

public sealed class RepresentativeFrameSelectionPolicy
{
    public const string LargestActiveHitboxAreaStrategy = "largest-active-hitbox-area";

    public string DefaultStrategy { get; init; } = LargestActiveHitboxAreaStrategy;
    public IReadOnlyList<string> PilotMoveScope { get; init; } = [];
    public IReadOnlyDictionary<string, RepresentativeFrameSelectionOverride> MoveOverrides { get; init; }
        = new Dictionary<string, RepresentativeFrameSelectionOverride>(StringComparer.OrdinalIgnoreCase);
    public string? DummyImagePath { get; init; }

    public bool IsMoveInScope(string characterId, string moveId)
    {
        if (PilotMoveScope.Count == 0)
        {
            return false;
        }

        return PilotMoveScope.Any(scope => MoveKeyMatches(scope, characterId, moveId));
    }

    public RepresentativeFrameSelectionOverride? FindOverride(string characterId, string moveId)
    {
        foreach (var pair in MoveOverrides)
        {
            if (MoveKeyMatches(pair.Key, characterId, moveId))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public IReadOnlyList<string> Validate(IEnumerable<string> knownMoveKeys)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(DefaultStrategy))
        {
            errors.Add("Representative frame default strategy is required.");
        }

        var known = new HashSet<string>(knownMoveKeys.Select(NormalizeMoveKey), StringComparer.OrdinalIgnoreCase);
        foreach (var scopedMove in PilotMoveScope)
        {
            if (!known.Contains(NormalizeMoveKey(scopedMove)))
            {
                errors.Add($"Representative frame pilot move does not resolve to a known move: {scopedMove}.");
            }
        }

        foreach (var (moveKey, moveOverride) in MoveOverrides)
        {
            if (!known.Contains(NormalizeMoveKey(moveKey)))
            {
                errors.Add($"Representative frame override does not resolve to a known move: {moveKey}.");
            }

            if (!string.IsNullOrWhiteSpace(moveOverride.SelectedFrame)
                && !string.IsNullOrWhiteSpace(moveOverride.SelectionStrategy))
            {
                errors.Add($"Representative frame override for {moveKey} cannot specify both selectedFrame and selectionStrategy.");
            }
        }

        if (!string.IsNullOrWhiteSpace(DummyImagePath) && !File.Exists(DummyImagePath))
        {
            errors.Add($"Representative frame dummy image was not found: {DummyImagePath}.");
        }

        return errors;
    }

    public static string BuildMoveKey(string characterId, string moveId)
        => $"{characterId.Trim()}/{moveId.Trim()}";

    public static string NormalizeMoveKey(string moveKey)
        => moveKey.Trim().Replace('\\', '/').ToLowerInvariant();

    private static bool MoveKeyMatches(string configuredMoveKey, string characterId, string moveId)
    {
        var normalizedConfigured = NormalizeMoveKey(configuredMoveKey);
        var normalizedMoveId = moveId.Trim().ToLowerInvariant();
        var normalizedFullKey = NormalizeMoveKey(BuildMoveKey(characterId, moveId));

        return string.Equals(normalizedConfigured, normalizedMoveId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedConfigured, normalizedFullKey, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class RepresentativeFrameSelectionOverride
{
    public string? SelectedFrame { get; init; }
    public string? SelectionStrategy { get; init; }
}
