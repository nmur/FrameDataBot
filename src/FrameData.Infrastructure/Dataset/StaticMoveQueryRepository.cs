using FrameData.Domain.Characters;
using FrameData.Domain.Datasets;
using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;

namespace FrameData.Infrastructure.Dataset;

public sealed class StaticMoveQueryRepository : IMoveQueryRepository
{
    private static readonly IReadOnlyDictionary<string, string[]> BuiltInCharacterAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["akuma"] = ["gouki"]
    };

    private readonly IReadOnlyDictionary<string, Character> _charactersByLookup;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<Move>> _movesByCharacterId;

    public StaticMoveQueryRepository(StaticFrameDataDataset dataset)
    {
        _charactersByLookup = BuildCharacterLookup(dataset.Characters);
        _movesByCharacterId = dataset.Moves
            .GroupBy(move => move.CharacterId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Move>)group
                    .OrderBy(move => move.DisplayOrder ?? int.MaxValue)
                    .ThenBy(move => move.Section, StringComparer.Ordinal)
                    .ThenBy(move => move.CanonicalName, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public Task<bool> SupportsCharacterAsync(string character, CancellationToken cancellationToken = default)
        => Task.FromResult(TryResolveCharacter(character, out _));

    public Task<Move?> FindExactMoveAsync(string character, string move, CancellationToken cancellationToken = default)
    {
        if (!TryResolveCharacter(character, out var resolvedCharacter))
        {
            return Task.FromResult<Move?>(null);
        }

        var foundMove = GetCharacterMoves(resolvedCharacter)
            .FirstOrDefault(candidate => string.Equals(
                candidate.CanonicalName.Trim(),
                move.Trim(),
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(foundMove);
    }

    public Task<IReadOnlyList<Move>> GetMovesForCharacterAsync(
        string character,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveCharacter(character, out var resolvedCharacter))
        {
            return Task.FromResult<IReadOnlyList<Move>>([]);
        }

        return Task.FromResult(GetCharacterMoves(resolvedCharacter));
    }

    private IReadOnlyList<Move> GetCharacterMoves(Character character)
        => _movesByCharacterId.TryGetValue(character.Id, out var moves) ? moves : [];

    private bool TryResolveCharacter(string character, out Character resolvedCharacter)
    {
        if (_charactersByLookup.TryGetValue(NormaliseLookup(character), out resolvedCharacter!))
        {
            return true;
        }

        resolvedCharacter = null!;
        return false;
    }

    private static IReadOnlyDictionary<string, Character> BuildCharacterLookup(IEnumerable<Character> characters)
    {
        var lookup = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);
        foreach (var character in characters)
        {
            AddCharacterLookup(lookup, character.Id, character);
            AddCharacterLookup(lookup, character.Name, character);
            foreach (var alias in character.Aliases)
            {
                AddCharacterLookup(lookup, alias, character);
            }

            if (BuiltInCharacterAliases.TryGetValue(character.Id, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    AddCharacterLookup(lookup, alias, character);
                }
            }
        }

        return lookup;
    }

    private static void AddCharacterLookup(
        IDictionary<string, Character> lookup,
        string value,
        Character character)
    {
        var normalised = NormaliseLookup(value);
        if (normalised.Length > 0)
        {
            lookup.TryAdd(normalised, character);
        }
    }

    private static string NormaliseLookup(string value)
        => value.Trim().ToLowerInvariant();
}
