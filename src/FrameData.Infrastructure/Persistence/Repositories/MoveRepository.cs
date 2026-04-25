using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;
using Npgsql;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class MoveRepository : IMoveQueryRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public MoveRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> SupportsCharacterAsync(string character, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT EXISTS (
              SELECT 1
              FROM characters c
              WHERE lower(c.id) = lower(@character)
                 OR lower(c.name) = lower(@character)
                 OR EXISTS (
                   SELECT 1
                   FROM jsonb_array_elements_text(c.aliases) alias
                   WHERE lower(alias) = lower(@character)
                 )
            );
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("character", character);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<Move?> FindExactMoveAsync(string character, string move, CancellationToken cancellationToken = default)
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
            WHERE lower(m.canonical_name) = lower(@move)
              AND (
                lower(c.id) = lower(@character)
                OR lower(c.name) = lower(@character)
                OR EXISTS (
                  SELECT 1
                  FROM jsonb_array_elements_text(c.aliases) alias
                  WHERE lower(alias) = lower(@character)
                )
              )
            ORDER BY m.display_order NULLS LAST, m.canonical_name
            LIMIT 1;
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("character", character);
        command.Parameters.AddWithValue("move", move);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapMove(reader);
    }

    public async Task UpsertMovesAsync(string characterId, IReadOnlyCollection<Move> moves, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var deleteCommand = new NpgsqlCommand("DELETE FROM moves WHERE character_id = @character_id;", connection, transaction))
        {
            deleteCommand.Parameters.AddWithValue("character_id", characterId);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var move in moves)
        {
            const string insertSql = """
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
                )
                ON CONFLICT (character_id, section, canonical_name) DO UPDATE SET
                  id = EXCLUDED.id,
                  display_order = EXCLUDED.display_order,
                  source_move_id = EXCLUDED.source_move_id,
                  startup = EXCLUDED.startup,
                  active = EXCLUDED.active,
                  recovery = EXCLUDED.recovery,
                  on_hit = EXCLUDED.on_hit,
                  on_block = EXCLUDED.on_block,
                  frame_advantage = EXCLUDED.frame_advantage,
                  notes = EXCLUDED.notes;
                """;

            await using var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            AddMoveParameters(insertCommand, move);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Move>> GetByCharacterIdAsync(string characterId, CancellationToken cancellationToken = default)
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
            WHERE lower(m.character_id) = lower(@character_id)
            ORDER BY m.display_order NULLS LAST, m.section, m.canonical_name;
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("character_id", characterId);

        var moves = new List<Move>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            moves.Add(MapMove(reader));
        }

        return moves;
    }

    private static void AddMoveParameters(NpgsqlCommand command, Move move)
    {
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
    }

    private static Move MapMove(NpgsqlDataReader reader)
    {
        var displayOrderOrdinal = reader.GetOrdinal("display_order");
        var sourceMoveIdOrdinal = reader.GetOrdinal("source_move_id");

        return new Move
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
        };
    }

    private static string? GetNullableString(NpgsqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
