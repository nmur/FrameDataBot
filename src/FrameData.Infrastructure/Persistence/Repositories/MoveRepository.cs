using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class MoveRepository : IMoveQueryRepository
{
    private readonly List<Move> _moves =
    [
        new()
        {
            Id = "makoto-2mk",
            CharacterId = "makoto",
            Game = "sf3_3s",
            CharacterName = "makoto",
            Section = "Normals",
            CanonicalName = "2mk",
            FrameData = new MoveFrameData
            {
                Startup = "6",
                Active = "3",
                Recovery = "17",
                OnHit = "+1",
                OnBlock = "-2",
                FrameAdvantage = "-2"
            }
        }
    ];

    public Task<bool> SupportsCharacterAsync(string character, CancellationToken cancellationToken = default)
        => Task.FromResult(_moves.Any(m => string.Equals(m.CharacterName, character, StringComparison.OrdinalIgnoreCase)));

    public Task<Move?> FindExactMoveAsync(string character, string move, CancellationToken cancellationToken = default)
    {
        var found = _moves.FirstOrDefault(m =>
            string.Equals(m.CharacterName, character, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(m.CanonicalName, move, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(found);
    }

    public Task UpsertMovesAsync(string characterId, IReadOnlyCollection<Move> moves, CancellationToken cancellationToken = default)
    {
        _moves.RemoveAll(m => string.Equals(m.CharacterId, characterId, StringComparison.OrdinalIgnoreCase));
        _moves.AddRange(moves);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Move>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default)
    {
        var found = _moves
            .Where(m => string.Equals(m.CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IReadOnlyList<Move>>(found);
    }
}
