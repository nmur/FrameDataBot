using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using Npgsql;
using NpgsqlTypes;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class FrameDataDatasetRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public FrameDataDatasetRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task ReplaceAsync(
        IReadOnlyCollection<Character> characters,
        IReadOnlyCollection<Move> moves,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteAsync("DELETE FROM moves;", connection, transaction, cancellationToken);
        await ExecuteAsync("DELETE FROM characters;", connection, transaction, cancellationToken);

        foreach (var character in characters)
        {
            await InsertCharacterAsync(character, connection, transaction, cancellationToken);
        }

        foreach (var move in moves)
        {
            await InsertMoveAsync(move, connection, transaction, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(
        string sql,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertCharacterAsync(
        Character character,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO characters (id, game, name, aliases, source_character_id, display_order, updated_at)
            VALUES (@id, @game, @name, @aliases, @source_character_id, @display_order, @updated_at);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
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
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertMoveAsync(
        Move move,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO moves (
              id,
              character_id,
              section,
              canonical_name,
              display_order,
              source_move_id,
              startup,
              active,
              recovery,
              on_hit,
              on_block,
              frame_advantage,
              notes
            )
            VALUES (
              @id,
              @character_id,
              @section,
              @canonical_name,
              @display_order,
              @source_move_id,
              @startup,
              @active,
              @recovery,
              @on_hit,
              @on_block,
              @frame_advantage,
              @notes
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", move.Id);
        command.Parameters.AddWithValue("character_id", move.CharacterId);
        command.Parameters.AddWithValue("section", move.Section);
        command.Parameters.AddWithValue("canonical_name", move.CanonicalName);
        command.Parameters.AddWithValue("display_order", (object?)move.DisplayOrder ?? DBNull.Value);
        command.Parameters.AddWithValue("source_move_id", (object?)move.SourceMoveId ?? DBNull.Value);
        command.Parameters.AddWithValue("startup", (object?)move.FrameData.Startup ?? DBNull.Value);
        command.Parameters.AddWithValue("active", (object?)move.FrameData.Active ?? DBNull.Value);
        command.Parameters.AddWithValue("recovery", (object?)move.FrameData.Recovery ?? DBNull.Value);
        command.Parameters.AddWithValue("on_hit", (object?)move.FrameData.OnHit ?? DBNull.Value);
        command.Parameters.AddWithValue("on_block", (object?)move.FrameData.OnBlock ?? DBNull.Value);
        command.Parameters.AddWithValue("frame_advantage", (object?)move.FrameData.FrameAdvantage ?? DBNull.Value);
        command.Parameters.AddWithValue("notes", (object?)move.FrameData.Notes ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
