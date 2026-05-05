namespace FrameData.Shared.Contracts;

public sealed class StaticDatasetManifest
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string DatasetId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public string? SourceBaseUrl { get; init; }
    public int CharacterCount { get; init; }
    public int MoveCount { get; init; }
    public int MediaCount { get; init; }
    public IReadOnlyList<StaticDatasetManifestCharacter> Characters { get; init; } = [];
}

public sealed class StaticDatasetManifestCharacter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string File { get; init; }
    public int? SourceCharacterId { get; init; }
    public int DisplayOrder { get; init; }
    public int MoveCount { get; init; }
}

public sealed class StaticDatasetCharacterDocument
{
    public required StaticDatasetCharacter Character { get; init; }
    public IReadOnlyList<StaticDatasetMove> Moves { get; init; } = [];
}

public sealed class StaticDatasetCharacter
{
    public required string Id { get; init; }
    public required string Game { get; init; }
    public required string Name { get; init; }
    public int? SourceCharacterId { get; init; }
    public int DisplayOrder { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public IReadOnlyList<string> Aliases { get; init; } = [];
}

public sealed class StaticDatasetMove
{
    public required string Id { get; init; }
    public required string CharacterId { get; init; }
    public required string Section { get; init; }
    public required string CanonicalName { get; init; }
    public int? DisplayOrder { get; init; }
    public string? SourceMoveId { get; init; }
    public string? Motion { get; init; }
    public string? Damage { get; init; }
    public string? Stun { get; init; }
    public string? SourceHitboxPath { get; init; }
    public required StaticDatasetMoveFrameData FrameData { get; init; }
    public IReadOnlyList<StaticDatasetMoveMedia> Media { get; init; } = [];
}

public sealed class StaticDatasetMoveFrameData
{
    public string? Startup { get; init; }
    public string? Active { get; init; }
    public string? Recovery { get; init; }
    public string? OnHit { get; init; }
    public string? OnBlock { get; init; }
    public string? OnCrouchingHit { get; init; }
    public string? Notes { get; init; }
}

public sealed class StaticDatasetMoveMedia
{
    public required string Type { get; init; }
    public required string Path { get; init; }
    public string? SourceUrl { get; init; }
    public string? SourceFrameImageUrl { get; init; }
    public string? SelectedFrame { get; init; }
    public string? SelectionStrategy { get; init; }
    public int? ActiveHitboxArea { get; init; }
    public IReadOnlyList<string> OverlayHitboxes { get; init; } = [];
    public string? FallbackReason { get; init; }
    public DateTimeOffset? CapturedAt { get; init; }
    public string? CaptureStatus { get; init; }
}
