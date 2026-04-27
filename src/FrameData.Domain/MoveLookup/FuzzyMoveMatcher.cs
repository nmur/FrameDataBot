using FrameData.Domain.Moves;
using FuzzySharp;

namespace FrameData.Domain.MoveLookup;

public sealed class FuzzyMoveMatcher
{
    public const decimal MinimumScore = 80;
    public const decimal AmbiguityScoreDelta = 5;
    private const int PrefixPartialScore = 96;
    private const int MinimumPrefixPartialLength = 4;
    private const int PartialSubstringScoreCap = 92;

    private readonly AliasNormalizer _normalizer;

    public FuzzyMoveMatcher(AliasNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public IReadOnlyList<MatchCandidate> Rank(string query, IEnumerable<Move> moves, int limit = 5)
    {
        var normalizedQuery = _normalizer.Normalize(query);
        if (normalizedQuery.Length == 0)
        {
            return [];
        }

        return moves
            .Select(move => RankMove(normalizedQuery, move))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .GroupBy(candidate => candidate.MoveId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(candidate => candidate.Score).ThenBy(candidate => candidate.MatchedAlias).First())
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Move.DisplayOrder ?? int.MaxValue)
            .ThenBy(candidate => candidate.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select((candidate, index) => new MatchCandidate
            {
                MoveId = candidate.MoveId,
                CanonicalName = candidate.CanonicalName,
                Section = candidate.Section,
                MatchedAlias = candidate.MatchedAlias,
                AliasType = candidate.AliasType,
                Score = candidate.Score,
                Rank = index + 1,
                ThresholdPassed = candidate.Score >= MinimumScore,
                Move = candidate.Move
            })
            .ToArray();
    }

    public bool IsAmbiguous(IReadOnlyList<MatchCandidate> candidates)
    {
        var passingCandidates = candidates
            .Where(candidate => candidate.ThresholdPassed)
            .Take(2)
            .ToArray();

        if (passingCandidates.Length < 2)
        {
            return false;
        }

        return passingCandidates[0].Score - passingCandidates[1].Score <= AmbiguityScoreDelta;
    }

    private MatchCandidate? RankMove(string normalizedQuery, Move move)
    {
        return _normalizer.CreateAliases(move)
            .Select(alias => CreateCandidate(normalizedQuery, move, alias))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.AliasType is MoveAliasType.Canonical ? 1 : 0)
            .ThenBy(candidate => candidate.AliasType)
            .FirstOrDefault();
    }

    private static MatchCandidate CreateCandidate(string normalizedQuery, Move move, MoveAlias alias)
    {
        var score = CalculateScore(normalizedQuery, alias.NormalizedAlias);

        return new MatchCandidate
        {
            MoveId = move.Id,
            CanonicalName = move.CanonicalName,
            Section = move.Section,
            MatchedAlias = alias.NormalizedAlias,
            AliasType = alias.AliasType,
            Score = score,
            Rank = 0,
            ThresholdPassed = score >= MinimumScore,
            Move = move
        };
    }

    private static int CalculateScore(string normalizedQuery, string normalizedAlias)
    {
        if (normalizedQuery == normalizedAlias)
        {
            return 100;
        }

        if (normalizedQuery.Length >= MinimumPrefixPartialLength
            && normalizedAlias.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            return PrefixPartialScore;
        }

        var ratio = Fuzz.Ratio(normalizedQuery, normalizedAlias);
        var partialRatio = Fuzz.PartialRatio(normalizedQuery, normalizedAlias);
        var score = Math.Max(ratio, partialRatio);

        if (partialRatio == score
            && normalizedQuery.Length != normalizedAlias.Length
            && (normalizedAlias.Contains(normalizedQuery, StringComparison.Ordinal)
                || normalizedQuery.Contains(normalizedAlias, StringComparison.Ordinal)))
        {
            return Math.Min(score, PartialSubstringScoreCap);
        }

        return score;
    }
}
