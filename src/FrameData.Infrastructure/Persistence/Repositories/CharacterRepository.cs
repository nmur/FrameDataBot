using System.Text.Json;
using FrameData.Domain.Characters;
using Npgsql;
using NpgsqlTypes;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class CharacterRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public CharacterRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task UpsertAsync(Character character, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO characters (id, game, name, aliases, source_character_id, display_order, updated_at)
            VALUES (@id, @game, @name, @aliases, @source_character_id, @display_order, @updated_at)
            ON CONFLICT (id) DO UPDATE SET
              game = EXCLUDED.game,
              name = EXCLUDED.name,
              aliases = EXCLUDED.aliases,
              source_character_id = EXCLUDED.source_character_id,
              display_order = EXCLUDED.display_order,
              updated_at = EXCLUDED.updated_at;
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        AddCharacterParameters(command, character);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Character?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, game, name, aliases, source_character_id, display_order, updated_at
            FROM characters
            WHERE lower(id) = lower(@id)
            LIMIT 1;
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapCharacter(reader);
    }

    private static void AddCharacterParameters(NpgsqlCommand command, Character character)
    {
        command.Parameters.AddWithValue("id", character.Id);
        command.Parameters.AddWithValue("game", character.Game);
        command.Parameters.AddWithValue("name", character.Name);
        command.Parameters.Add(new NpgsqlParameter<string>("aliases", NpgsqlDbType.Jsonb)
        {
            Value = JsonSerializer.Serialize(character.Aliases)
        });
        command.Parameters.Add(new NpgsqlParameter("source_character_id", NpgsqlDbType.Integer)
        {
            Value = (object?)character.SourceCharacterId ?? DBNull.Value
        });
        command.Parameters.AddWithValue("display_order", character.DisplayOrder);
        command.Parameters.AddWithValue("updated_at", character.UpdatedAt ?? DateTimeOffset.UtcNow);
    }

    private static Character MapCharacter(NpgsqlDataReader reader)
    {
        var aliasesJson = reader.GetString(reader.GetOrdinal("aliases"));
        var aliases = JsonSerializer.Deserialize<string[]>(aliasesJson) ?? [];
        var sourceCharacterIdOrdinal = reader.GetOrdinal("source_character_id");
        var updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new Character
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            Game = reader.GetString(reader.GetOrdinal("game")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Aliases = aliases,
            SourceCharacterId = reader.IsDBNull(sourceCharacterIdOrdinal) ? null : reader.GetInt32(sourceCharacterIdOrdinal),
            DisplayOrder = reader.GetInt32(reader.GetOrdinal("display_order")),
            UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? null : reader.GetFieldValue<DateTimeOffset>(updatedAtOrdinal)
        };
    }
}
