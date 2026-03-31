namespace FrameData.Domain.MoveLookup;

public sealed class ExactMoveLookupService
{
    public const string DefaultGame = "sf3_3s";

    private readonly IMoveQueryRepository _repository;

    public ExactMoveLookupService(IMoveQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<MoveLookupResult> LookupAsync(string? game, string character, string move, CancellationToken cancellationToken = default)
    {
        var normalizedGame = string.IsNullOrWhiteSpace(game) ? DefaultGame : game.Trim().ToLowerInvariant();
        var normalizedCharacter = character.Trim();
        var normalizedMove = move.Trim();

        if (!await _repository.SupportsGameAsync(normalizedGame, cancellationToken))
        {
            return MoveLookupResult.UnsupportedGame(normalizedGame);
        }

        if (!await _repository.SupportsCharacterAsync(normalizedGame, normalizedCharacter, cancellationToken))
        {
            return MoveLookupResult.UnsupportedCharacter(normalizedCharacter);
        }

        var foundMove = await _repository.FindExactMoveAsync(normalizedGame, normalizedCharacter, normalizedMove, cancellationToken);
        if (foundMove is null)
        {
            return MoveLookupResult.NotFound(normalizedMove);
        }

        return MoveLookupResult.Found(foundMove);
    }
}
