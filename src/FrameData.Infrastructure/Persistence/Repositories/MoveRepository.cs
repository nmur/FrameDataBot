using FrameData.Domain.MoveLookup;
using FrameData.Domain.Moves;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class MoveRepository : IMoveQueryRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<MoveRepository> _logger;

    public MoveRepository(DbConnectionFactory connectionFactory, ILogger<MoveRepository>? logger = null)
    {
        _connectionFactory = connectionFactory;
        _logger = logger ?? NullLogger<MoveRepository>.Instance;
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
        var isSupported = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        _logger.LogDebug(
            "Character support lookup for input {Character} returned {IsSupported}.",
            character,
            isSupported);

        return isSupported;
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
            await LogMoveMissDiagnosticsAsync(character, move, cancellationToken);
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
                ON CONFLICT (id) DO UPDATE SET
                  display_order = EXCLUDED.display_order,
                  character_id = EXCLUDED.character_id,
                  section = EXCLUDED.section,
                  canonical_name = EXCLUDED.canonical_name,
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

    public async Task<IReadOnlyList<Move>> GetMovesForCharacterAsync(string character, CancellationToken cancellationToken = default)
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
            WHERE lower(c.id) = lower(@character)
               OR lower(c.name) = lower(@character)
               OR EXISTS (
                 SELECT 1
                 FROM jsonb_array_elements_text(c.aliases) alias
                 WHERE lower(alias) = lower(@character)
               )
            ORDER BY m.display_order NULLS LAST, m.section, m.canonical_name;
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("character", character);

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

    private async Task LogMoveMissDiagnosticsAsync(string character, string move, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = _connectionFactory.CreateOpenConnection();
            var summary = await GetCharacterMoveSummaryAsync(connection, character, cancellationToken);
            if (summary is null)
            {
                _logger.LogWarning(
                    "Move lookup missed for {Character} {MoveInput}; no matching character row was visible during miss diagnostics.",
                    character,
                    move);
                return;
            }

            var trimMatch = await HasTrimmedMoveMatchAsync(connection, summary.CharacterId, move, cancellationToken);
            var samples = await GetMoveNameSamplesAsync(connection, summary.CharacterId, cancellationToken);
            var sampleText = samples.Count == 0 ? "<none>" : string.Join(", ", samples);

            _logger.LogWarning(
                "Move lookup missed for {Character} {MoveInput}; matched character {CharacterId} ({CharacterName}) has {MoveCount} stored move(s). Trimmed-name match: {HasTrimmedMatch}. Sample stored moves: {StoredMoveSamples}.",
                character,
                move,
                summary.CharacterId,
                summary.CharacterName,
                summary.MoveCount,
                trimMatch,
                sampleText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Move lookup miss diagnostics failed for character {Character} and move input {MoveInput}.",
                character,
                move);
        }
    }

    private static async Task<CharacterMoveSummary?> GetCharacterMoveSummaryAsync(
        NpgsqlConnection connection,
        string character,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
              c.id,
              c.name,
              COUNT(m.id) AS move_count
            FROM characters c
            LEFT JOIN moves m ON m.character_id = c.id
            WHERE lower(c.id) = lower(@character)
               OR lower(c.name) = lower(@character)
               OR EXISTS (
                 SELECT 1
                 FROM jsonb_array_elements_text(c.aliases) alias
                 WHERE lower(alias) = lower(@character)
               )
            GROUP BY c.id, c.name, c.display_order
            ORDER BY c.display_order, c.id
            LIMIT 1;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("character", character);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterMoveSummary(
            reader.GetString(reader.GetOrdinal("id")),
            reader.GetString(reader.GetOrdinal("name")),
            reader.GetInt64(reader.GetOrdinal("move_count")));
    }

    private static async Task<bool> HasTrimmedMoveMatchAsync(
        NpgsqlConnection connection,
        string characterId,
        string move,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
              SELECT 1
              FROM moves
              WHERE character_id = @character_id
                AND lower(trim(canonical_name)) = lower(trim(@move))
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("character_id", characterId);
        command.Parameters.AddWithValue("move", move);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static async Task<IReadOnlyList<string>> GetMoveNameSamplesAsync(
        NpgsqlConnection connection,
        string characterId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT section, canonical_name
            FROM moves
            WHERE character_id = @character_id
            ORDER BY display_order NULLS LAST, section, canonical_name
            LIMIT 20;
            """;

        var samples = new List<string>();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("character_id", characterId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            samples.Add($"{reader.GetString(reader.GetOrdinal("section"))}/{reader.GetString(reader.GetOrdinal("canonical_name"))}");
        }

        return samples;
    }

    private sealed record CharacterMoveSummary(string CharacterId, string CharacterName, long MoveCount);
}
