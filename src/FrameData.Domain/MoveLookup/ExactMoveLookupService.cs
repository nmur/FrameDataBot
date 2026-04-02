namespace FrameData.Domain.MoveLookup;

public sealed class ExactMoveLookupService
{
    private readonly IMoveQueryRepository _repository;

    public ExactMoveLookupService(IMoveQueryRepository repository)
    {
        _repository = repository;
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
            return MoveLookupResult.NotFound(normalizedMove);
        }

        return MoveLookupResult.Found(foundMove);
    }
}
