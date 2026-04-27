using FrameData.Domain.Characters;
using FrameData.Domain.Moves;

namespace FrameData.Domain.Datasets;

public sealed class StaticFrameDataDataset
{
    public required string DatasetId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public string? SourceBaseUrl { get; init; }
    public required IReadOnlyList<Character> Characters { get; init; }
    public required IReadOnlyList<Move> Moves { get; init; }
    public int MediaCount { get; init; }
}
