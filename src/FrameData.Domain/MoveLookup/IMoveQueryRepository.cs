using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public interface IMoveQueryRepository
{
    Task<bool> SupportsGameAsync(string game, CancellationToken cancellationToken = default);
    Task<bool> SupportsCharacterAsync(string game, string character, CancellationToken cancellationToken = default);
    Task<Move?> FindExactMoveAsync(string game, string character, string move, CancellationToken cancellationToken = default);
}
