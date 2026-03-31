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
}
