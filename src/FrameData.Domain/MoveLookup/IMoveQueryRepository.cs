using FrameData.Domain.Moves;

namespace FrameData.Domain.MoveLookup;

public interface IMoveQueryRepository
{
    Task<bool> SupportsCharacterAsync(string character, CancellationToken cancellationToken = default);
    Task<Move?> FindExactMoveAsync(string character, string move, CancellationToken cancellationToken = default);
}
