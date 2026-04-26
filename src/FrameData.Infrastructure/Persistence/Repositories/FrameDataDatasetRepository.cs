using System.Text.Json;
using FrameData.Domain.Characters;
using FrameData.Domain.Moves;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class FrameDataDatasetRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<FrameDataDatasetRepository> _logger;

    public FrameDataDatasetRepository(
        DbConnectionFactory connectionFactory,
        ILogger<FrameDataDatasetRepository>? logger = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger ?? NullLogger<FrameDataDatasetRepository>.Instance;
    }

    public async Task ReplaceAsync(
        IReadOnlyCollection<Character> characters,
        IReadOnlyCollection<Move> moves,
        CancellationToken cancellationToken = default)
    {
        try
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

            _logger.LogInformation(
                "Committed frame data dataset replacement with {CharacterCount} character(s) and {MoveCount} move(s).",
                characters.Count,
                moves.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to replace frame data dataset with {CharacterCount} character(s) and {MoveCount} move(s). First move: {FirstMoveId}.",
                characters.Count,
                moves.Count,
                moves.FirstOrDefault()?.Id ?? "<none>");
            throw;
        }
    }

    public async Task<FrameDataDataset> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateOpenConnection();
        var characters = await GetCharactersAsync(connection, cancellationToken);
        var moves = await GetMovesAsync(connection, cancellationToken);
        return new FrameDataDataset(characters, moves);
    }

    private static async Task<IReadOnlyList<Character>> GetCharactersAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, game, name, aliases, source_character_id, display_order, updated_at
            FROM characters
            ORDER BY display_order, id;
            """;

        var characters = new List<Character>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var aliasesJson = reader.GetString(reader.GetOrdinal("aliases"));
            var aliases = JsonSerializer.Deserialize<string[]>(aliasesJson) ?? [];
            var sourceCharacterIdOrdinal = reader.GetOrdinal("source_character_id");
            var updatedAtOrdinal = reader.GetOrdinal("updated_at");

            characters.Add(new Character
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                Game = reader.GetString(reader.GetOrdinal("game")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Aliases = aliases,
                SourceCharacterId = reader.IsDBNull(sourceCharacterIdOrdinal) ? null : reader.GetInt32(sourceCharacterIdOrdinal),
                DisplayOrder = reader.GetInt32(reader.GetOrdinal("display_order")),
                UpdatedAt = reader.IsDBNull(updatedAtOrdinal) ? null : reader.GetFieldValue<DateTimeOffset>(updatedAtOrdinal)
            });
        }

        return characters;
    }

    private static async Task<IReadOnlyList<Move>> GetMovesAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              m.id,
              m.character_id,
              c.game,
              c.name AS character_name,
              m.section,
              m.canonical_name,
              m.display_order,
              m.source_move_id,
              m.startup,
              m.active,
              m.recovery,
              m.on_hit,
              m.on_block,
              m.frame_advantage,
              m.notes
            FROM moves m
            JOIN characters c ON c.id = m.character_id
            ORDER BY c.display_order, m.display_order NULLS LAST, m.section, m.canonical_name;
            """;

        var moves = new List<Move>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var displayOrderOrdinal = reader.GetOrdinal("display_order");
            var sourceMoveIdOrdinal = reader.GetOrdinal("source_move_id");

            moves.Add(new Move
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                CharacterId = reader.GetString(reader.GetOrdinal("character_id")),
                Game = reader.GetString(reader.GetOrdinal("game")),
                CharacterName = reader.GetString(reader.GetOrdinal("character_name")),
                Section = reader.GetString(reader.GetOrdinal("section")),
                CanonicalName = reader.GetString(reader.GetOrdinal("canonical_name")),
                DisplayOrder = reader.IsDBNull(displayOrderOrdinal) ? null : reader.GetInt32(displayOrderOrdinal),
                SourceMoveId = reader.IsDBNull(sourceMoveIdOrdinal) ? null : reader.GetString(sourceMoveIdOrdinal),
                FrameData = new MoveFrameData
                {
                    Startup = GetNullableString(reader, "startup"),
                    Active = GetNullableString(reader, "active"),
                    Recovery = GetNullableString(reader, "recovery"),
                    OnHit = GetNullableString(reader, "on_hit"),
                    OnBlock = GetNullableString(reader, "on_block"),
                    FrameAdvantage = GetNullableString(reader, "frame_advantage"),
                    Notes = GetNullableString(reader, "notes")
                }
            });
        }

        return moves;
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

    private static string? GetNullableString(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}

public sealed record FrameDataDataset(IReadOnlyList<Character> Characters, IReadOnlyList<Move> Moves);
