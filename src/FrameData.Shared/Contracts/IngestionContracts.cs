namespace FrameData.Shared.Contracts;

public sealed class IngestionRunRequest
{
    public IReadOnlyList<string> CharacterIds { get; init; } = [];
}

public sealed class IngestionAcceptedResponse
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
    public required string Scope { get; init; }
    public int CharactersQueued { get; init; }
}

public sealed class IngestionRunResponse
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int CharactersProcessed { get; init; }
    public int MovesProcessed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<IngestionRunCharacterStatusContract> CharacterStatuses { get; init; } = [];
}

public sealed class IngestionRunCharacterStatusContract
{
    public required string CharacterId { get; init; }
    public int SourceCharacterId { get; init; }
    public required string Status { get; init; }
    public int MovesProcessed { get; init; }
    public string? Error { get; init; }
}
