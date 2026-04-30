using FrameData.Domain.MoveLookup;
using FrameData.Shared.Contracts;

namespace FrameData.Api.Responses;

public sealed class MoveDisambiguationResponseFactory
{
    public MoveAmbiguousResponse Create(IReadOnlyList<MatchCandidate> candidates)
    {
        return new MoveAmbiguousResponse
        {
            Message = "Multiple moves matched. Try a more specific move name.",
            Candidates = candidates
                .OrderBy(candidate => candidate.Rank)
                .Select(candidate => new MoveCandidate
                {
                    MoveName = candidate.CanonicalName,
                    Section = candidate.Section,
                    Score = candidate.Score
                })
                .ToArray()
        };
    }
}
