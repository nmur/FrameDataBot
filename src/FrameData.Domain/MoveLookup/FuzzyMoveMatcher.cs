using FrameData.Domain.Moves;
using FuzzySharp;

namespace FrameData.Domain.MoveLookup;

public sealed class FuzzyMoveMatcher
{
    public const decimal MinimumScore = 80;
    public const decimal AmbiguityScoreDelta = 5;
    private const int PrefixPartialScore = 98;
    private const int MinimumPrefixPartialLength = 4;
    private const int PartialSubstringScoreCap = 92;

    private readonly AliasNormaliser _normaliser;

    public FuzzyMoveMatcher(AliasNormaliser normaliser)
    {
        _normaliser = normaliser;
    }

    public IReadOnlyList<MatchCandidate> Rank(string query, IEnumerable<Move> moves, int limit = 5)
    {
        var normalisedQuery = _normaliser.Normalise(query);
        if (normalisedQuery.Length == 0)
        {
            return [];
        }

        var moveList = moves as IReadOnlyList<Move> ?? moves.ToArray();
        return moveList
            .Select(move => RankMove(normalisedQuery, move, moveList))
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

    private MatchCandidate? RankMove(string normalisedQuery, Move move, IReadOnlyList<Move> characterMoves)
    {
        return _normaliser.CreateAliases(move, characterMoves)
            .Select(alias => CreateCandidate(normalisedQuery, move, alias))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.AliasType is MoveAliasType.Canonical ? 1 : 0)
            .ThenBy(candidate => candidate.AliasType)
            .FirstOrDefault();
    }

    private static MatchCandidate CreateCandidate(string normalisedQuery, Move move, MoveAlias alias)
    {
        var score = CalculateScore(normalisedQuery, alias.NormalisedAlias);

        return new MatchCandidate
        {
            MoveId = move.Id,
            CanonicalName = move.CanonicalName,
            Section = move.Section,
            MatchedAlias = alias.NormalisedAlias,
            AliasType = alias.AliasType,
            Score = score,
            Rank = 0,
            ThresholdPassed = score >= MinimumScore,
            Move = move
        };
    }

    private static int CalculateScore(string normalisedQuery, string normalisedAlias)
    {
        if (normalisedQuery == normalisedAlias)
        {
            return 100;
        }

        if (normalisedQuery.Length >= MinimumPrefixPartialLength
            && normalisedAlias.StartsWith(normalisedQuery, StringComparison.Ordinal))
        {
            return PrefixPartialScore;
        }

        var ratio = Fuzz.Ratio(normalisedQuery, normalisedAlias);
        var partialRatio = Fuzz.PartialRatio(normalisedQuery, normalisedAlias);
        var score = Math.Max(ratio, partialRatio);

        if (partialRatio == score
            && normalisedQuery.Length != normalisedAlias.Length
            && (normalisedAlias.Contains(normalisedQuery, StringComparison.Ordinal)
                || normalisedQuery.Contains(normalisedAlias, StringComparison.Ordinal)))
        {
            return Math.Min(score, PartialSubstringScoreCap);
        }

        return score;
    }
}
