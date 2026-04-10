namespace FrameData.Shared.Contracts;

public sealed class IngestionAcceptedResponse
{
    public required string RunId { get; init; }
    public required string Status { get; init; }
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
}
