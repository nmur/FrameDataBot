using FrameData.Domain.Media;
using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;

namespace FrameData.Ingestion.Customization;

public sealed class CustomMoveOverlay
{
    private static readonly IReadOnlyList<CustomMoveDefinition> Definitions =
    [
        new CustomMoveDefinition
        {
            Id = "oro-custom-peanut",
            CharacterId = "oro",
            CanonicalName = "Indecent Exposure",
            SourceMoveName = "crouching roundhouse",
            SectionOverride = "Specials",
            SelectedFrame = "22",
            OverlayHitboxes = [],
            DamageOverride = "69"
        }
    ];

    private readonly AliasNormaliser _normaliser = new();

    public IReadOnlyList<Move> Apply(string characterId, IReadOnlyList<Move> moves)
    {
        var updatedMoves = moves.ToList();
        foreach (var definition in Definitions.Where(definition => CharacterMatches(definition, characterId)))
        {
            if (updatedMoves.Any(move => IsCustomMove(definition, move)))
            {
                continue;
            }

            var sourceMove = FindSourceMove(definition, updatedMoves);
            if (sourceMove is null)
            {
                continue;
            }

            updatedMoves.Add(CloneMove(definition, sourceMove, GetNextDisplayOrder(updatedMoves)));
        }

        return updatedMoves;
    }

    public RepresentativeFrameSelectionPolicy ApplyRepresentativeFrameOverrides(
        RepresentativeFrameSelectionPolicy policy,
        IReadOnlyList<Move> moves)
    {
        var moveOverrides = new Dictionary<string, RepresentativeFrameSelectionOverride>(
            policy.MoveOverrides,
            StringComparer.OrdinalIgnoreCase);
        var pilotMoveScope = policy.PilotMoveScope.ToList();

        foreach (var definition in Definitions)
        {
            var customMove = moves.FirstOrDefault(move => IsCustomMove(definition, move));
            var sourceMove = FindSourceMove(definition, moves);
            if (customMove is null || sourceMove is null)
            {
                continue;
            }

            moveOverrides[RepresentativeFrameSelectionPolicy.BuildMoveKey(
                customMove.CharacterId,
                customMove.Id)] = new RepresentativeFrameSelectionOverride
                {
                    SelectedFrame = definition.SelectedFrame,
                    OverlayHitboxes = definition.OverlayHitboxes
                };

            if (policy.PilotMoveScope.Count > 0
                && policy.IsMoveInScope(sourceMove.CharacterId, sourceMove.Id)
                && !policy.IsMoveInScope(customMove.CharacterId, customMove.Id))
            {
                var customMoveKey = RepresentativeFrameSelectionPolicy.BuildMoveKey(
                    customMove.CharacterId,
                    customMove.Id);
                if (!pilotMoveScope.Any(scope =>
                        string.Equals(
                            RepresentativeFrameSelectionPolicy.NormaliseMoveKey(scope),
                            RepresentativeFrameSelectionPolicy.NormaliseMoveKey(customMoveKey),
                            StringComparison.OrdinalIgnoreCase)))
                {
                    pilotMoveScope.Add(customMoveKey);
                }
            }
        }

        return new RepresentativeFrameSelectionPolicy
        {
            DefaultStrategy = policy.DefaultStrategy,
            PilotMoveScope = pilotMoveScope,
            MoveOverrides = moveOverrides,
            DummyImagePath = policy.DummyImagePath
        };
    }

    private Move? FindSourceMove(CustomMoveDefinition definition, IEnumerable<Move> moves)
        => moves.FirstOrDefault(move => IsSourceMove(definition, move));

    private bool IsSourceMove(CustomMoveDefinition definition, Move move)
    {
        return CharacterMatches(definition, move.CharacterId)
            && !IsCustomMove(definition, move)
            && string.Equals(
                _normaliser.Normalise(move.CanonicalName),
                _normaliser.Normalise(definition.SourceMoveName),
                StringComparison.Ordinal);
    }

    private static bool IsCustomMove(CustomMoveDefinition definition, Move move)
        => string.Equals(move.Id, definition.Id, StringComparison.OrdinalIgnoreCase)
            || (CharacterMatches(definition, move.CharacterId)
                && string.Equals(move.CanonicalName, definition.CanonicalName, StringComparison.Ordinal));

    private static bool CharacterMatches(CustomMoveDefinition definition, string characterId)
        => string.Equals(definition.CharacterId, characterId, StringComparison.OrdinalIgnoreCase);

    private static Move CloneMove(CustomMoveDefinition definition, Move sourceMove, int displayOrder)
        => new()
        {
            Id = definition.Id,
            CharacterId = sourceMove.CharacterId,
            Game = sourceMove.Game,
            CharacterName = sourceMove.CharacterName,
            SourceCharacterId = sourceMove.SourceCharacterId,
            SourceBaseUrl = sourceMove.SourceBaseUrl,
            Section = definition.SectionOverride ?? sourceMove.Section,
            CanonicalName = definition.CanonicalName,
            DisplayOrder = displayOrder,
            SourceMoveId = sourceMove.SourceMoveId,
            SourceHitboxPath = sourceMove.SourceHitboxPath,
            Motion = sourceMove.Motion,
            Damage = definition.DamageOverride ?? sourceMove.Damage,
            Stun = sourceMove.Stun,
            FrameData = new MoveFrameData
            {
                Startup = sourceMove.FrameData.Startup,
                Active = sourceMove.FrameData.Active,
                Recovery = sourceMove.FrameData.Recovery,
                OnHit = sourceMove.FrameData.OnHit,
                OnBlock = sourceMove.FrameData.OnBlock,
                OnCrouchingHit = sourceMove.FrameData.OnCrouchingHit,
                Notes = sourceMove.FrameData.Notes
            },
            Media = []
        };

    private static int GetNextDisplayOrder(IEnumerable<Move> moves)
        => moves
            .Where(move => move.DisplayOrder.HasValue)
            .Select(move => move.DisplayOrder!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

    private sealed class CustomMoveDefinition
    {
        public required string Id { get; init; }
        public required string CharacterId { get; init; }
        public required string CanonicalName { get; init; }
        public required string SourceMoveName { get; init; }
        public required string SelectedFrame { get; init; }
        public string? SectionOverride { get; init; }
        public IReadOnlyList<string>? OverlayHitboxes { get; init; }
        public string? DamageOverride { get; init; }
    }
}
