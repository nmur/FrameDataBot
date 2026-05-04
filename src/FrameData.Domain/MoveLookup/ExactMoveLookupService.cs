namespace FrameData.Domain.MoveLookup;

public sealed class ExactMoveLookupService
{
    private readonly IMoveQueryRepository _repository;
    private readonly FuzzyMoveMatcher _matcher;

    public ExactMoveLookupService(IMoveQueryRepository repository, FuzzyMoveMatcher? matcher = null)
    {
        _repository = repository;
        _matcher = matcher ?? new FuzzyMoveMatcher(new AliasNormaliser());
    }

    public async Task<MoveLookupResult> LookupAsync(string character, string move, CancellationToken cancellationToken = default)
    {
        var normalisedCharacter = character.Trim();
        var normalisedMove = move.Trim();

        if (!await _repository.SupportsCharacterAsync(normalisedCharacter, cancellationToken))
        {
            return MoveLookupResult.UnsupportedCharacter(normalisedCharacter);
        }

        var foundMove = await _repository.FindExactMoveAsync(normalisedCharacter, normalisedMove, cancellationToken);
        if (foundMove is null)
        {
            var moves = await _repository.GetMovesForCharacterAsync(normalisedCharacter, cancellationToken);
            var candidates = _matcher.Rank(normalisedMove, moves);
            var passingCandidates = candidates
                .Where(candidate => candidate.ThresholdPassed)
                .ToArray();

            if (passingCandidates.Length == 0)
            {
                return MoveLookupResult.NotFound(normalisedMove);
            }

            if (_matcher.IsAmbiguous(candidates))
            {
                return MoveLookupResult.Ambiguous(normalisedMove, passingCandidates);
            }

            return MoveLookupResult.Found(passingCandidates[0].Move, passingCandidates[0].MatchedBy);
        }

        return MoveLookupResult.Found(foundMove);
    }
}
