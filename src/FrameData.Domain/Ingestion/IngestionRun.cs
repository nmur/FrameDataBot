namespace FrameData.Domain.Ingestion;

public sealed class IngestionRun
{
    public required string Id { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
    public required string Status { get; set; }
    public int CharactersProcessed { get; set; }
    public int MovesProcessed { get; set; }
    public List<string> Errors { get; init; } = [];
    public List<IngestionRunCharacterStatus> CharacterStatuses { get; init; } = [];
}

public sealed class IngestionRunCharacterStatus
{
    public required string CharacterId { get; init; }
    public int SourceCharacterId { get; init; }
    public required string Status { get; init; }
    public int MovesProcessed { get; init; }
    public string? Error { get; init; }
}
