namespace FrameData.Domain.Media;

public sealed class RepresentativeFrameSelectionPolicy
{
    public const string LargestActiveHitboxAreaStrategy = "largest-active-hitbox-area";
    public const string LargestActiveThrowHitboxAreaStrategy = "largest-active-throw-hitbox-area";

    public string DefaultStrategy { get; init; } = LargestActiveHitboxAreaStrategy;
    public IReadOnlyList<string> PilotMoveScope { get; init; } = [];
    public IReadOnlyDictionary<string, RepresentativeFrameSelectionOverride> MoveOverrides { get; init; }
        = new Dictionary<string, RepresentativeFrameSelectionOverride>(StringComparer.OrdinalIgnoreCase);
    public string? DummyImagePath { get; init; }

    public bool IsMoveInScope(string characterId, string moveId)
    {
        if (PilotMoveScope.Count == 0)
        {
            return true;
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

        var known = new HashSet<string>(knownMoveKeys.Select(NormaliseMoveKey), StringComparer.OrdinalIgnoreCase);
        var knownCharacters = new HashSet<string>(
            known
                .Select(GetCharacterScope)
                .Where(character => !string.IsNullOrWhiteSpace(character))
                .Select(character => character!),
            StringComparer.OrdinalIgnoreCase);
        foreach (var scopedMove in PilotMoveScope)
        {
            var normalisedScope = NormaliseMoveKey(scopedMove);
            if (!known.Contains(normalisedScope) && !CharacterScopeMatchesKnownCharacter(normalisedScope, knownCharacters))
            {
                errors.Add($"Representative frame media scope does not resolve to a known move or character: {scopedMove}.");
            }
        }

        foreach (var (moveKey, moveOverride) in MoveOverrides)
        {
            if (!known.Contains(NormaliseMoveKey(moveKey)))
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

    public static string NormaliseMoveKey(string moveKey)
        => moveKey.Trim().Replace('\\', '/').ToLowerInvariant();

    private static bool MoveKeyMatches(string configuredMoveKey, string characterId, string moveId)
    {
        var normalisedConfigured = NormaliseMoveKey(configuredMoveKey);
        var normalisedCharacterId = characterId.Trim().ToLowerInvariant();
        var normalisedMoveId = moveId.Trim().ToLowerInvariant();
        var normalisedFullKey = NormaliseMoveKey(BuildMoveKey(characterId, moveId));

        return string.Equals(normalisedConfigured, normalisedCharacterId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalisedConfigured, $"{normalisedCharacterId}/*", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalisedConfigured, normalisedMoveId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalisedConfigured, normalisedFullKey, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CharacterScopeMatchesKnownCharacter(string normalisedScope, IReadOnlySet<string> knownCharacters)
    {
        var characterScope = normalisedScope.EndsWith("/*", StringComparison.Ordinal)
            ? normalisedScope[..^2]
            : normalisedScope;

        return knownCharacters.Contains(characterScope);
    }

    private static string? GetCharacterScope(string normalisedMoveKey)
    {
        var separatorIndex = normalisedMoveKey.IndexOf('/');
        return separatorIndex <= 0 ? null : normalisedMoveKey[..separatorIndex];
    }
}

public sealed class RepresentativeFrameSelectionOverride
{
    public string? SelectedFrame { get; init; }
    public string? SelectionStrategy { get; init; }
}
