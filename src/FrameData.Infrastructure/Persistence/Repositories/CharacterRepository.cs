using FrameData.Domain.Characters;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class CharacterRepository
{
    private readonly Dictionary<string, Character> _characters = new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(Character character, CancellationToken cancellationToken = default)
    {
        _characters[character.Id] = character;
        return Task.CompletedTask;
    }

    public Task<Character?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _characters.TryGetValue(id, out var character);
        return Task.FromResult(character);
    }
}
