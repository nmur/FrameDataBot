namespace FrameData.Domain.MoveLookup;

public sealed class ExactMoveLookupService
{
    private readonly IMoveQueryRepository _repository;
    private readonly FuzzyMoveMatcher _matcher;

    public ExactMoveLookupService(IMoveQueryRepository repository, FuzzyMoveMatcher? matcher = null)
    {
        _repository = repository;
        _matcher = matcher ?? new FuzzyMoveMatcher(new AliasNormalizer());
    }

    public async Task<MoveLookupResult> LookupAsync(string character, string move, CancellationToken cancellationToken = default)
    {
        var normalizedCharacter = character.Trim();
        var normalizedMove = move.Trim();

        if (!await _repository.SupportsCharacterAsync(normalizedCharacter, cancellationToken))
        {
            return MoveLookupResult.UnsupportedCharacter(normalizedCharacter);
        }

        var foundMove = await _repository.FindExactMoveAsync(normalizedCharacter, normalizedMove, cancellationToken);
        if (foundMove is null)
        {
            var moves = await _repository.GetMovesForCharacterAsync(normalizedCharacter, cancellationToken);
            var candidates = _matcher.Rank(normalizedMove, moves);
            var passingCandidates = candidates
                .Where(candidate => candidate.ThresholdPassed)
                .ToArray();

            if (passingCandidates.Length == 0)
            {
                return MoveLookupResult.NotFound(normalizedMove);
            }

            if (_matcher.IsAmbiguous(candidates))
            {
                return MoveLookupResult.Ambiguous(normalizedMove, passingCandidates);
            }

            return MoveLookupResult.Found(passingCandidates[0].Move, passingCandidates[0].MatchedBy);
        }

        return MoveLookupResult.Found(foundMove);
    }
}
