using System.Text.Json;
using FrameData.Domain.Ingestion;
using Npgsql;
using NpgsqlTypes;

namespace FrameData.Infrastructure.Persistence.Repositories;

public sealed class IngestionRunRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public IngestionRunRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveAsync(IngestionRun run, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO ingestion_runs (
              id,
              started_at,
              completed_at,
              status,
              characters_processed,
              moves_processed,
              errors
            )
            VALUES (
              @id,
              @started_at,
              @completed_at,
              @status,
              @characters_processed,
              @moves_processed,
              @errors
            )
            ON CONFLICT (id) DO UPDATE SET
              started_at = EXCLUDED.started_at,
              completed_at = EXCLUDED.completed_at,
              status = EXCLUDED.status,
              characters_processed = EXCLUDED.characters_processed,
              moves_processed = EXCLUDED.moves_processed,
              errors = EXCLUDED.errors;
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var command = new NpgsqlCommand(sql, connection, transaction))
        {
            command.Parameters.AddWithValue("id", run.Id);
            command.Parameters.AddWithValue("started_at", run.StartedAt);
            command.Parameters.AddWithValue("completed_at", (object?)run.CompletedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("status", run.Status);
            command.Parameters.AddWithValue("characters_processed", run.CharactersProcessed);
            command.Parameters.AddWithValue("moves_processed", run.MovesProcessed);
            command.Parameters.Add(new NpgsqlParameter<string>("errors", NpgsqlDbType.Jsonb)
            {
                Value = JsonSerializer.Serialize(run.Errors)
            });
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteStatuses = new NpgsqlCommand(
            "DELETE FROM ingestion_run_character_statuses WHERE run_id = @run_id;",
            connection,
            transaction))
        {
            deleteStatuses.Parameters.AddWithValue("run_id", run.Id);
            await deleteStatuses.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var status in run.CharacterStatuses)
        {
            const string statusSql = """
                INSERT INTO ingestion_run_character_statuses (
                  run_id,
                  character_id,
                  source_character_id,
                  status,
                  moves_processed,
                  error
                )
                VALUES (
                  @run_id,
                  @character_id,
                  @source_character_id,
                  @status,
                  @moves_processed,
                  @error
                );
                """;

            await using var statusCommand = new NpgsqlCommand(statusSql, connection, transaction);
            statusCommand.Parameters.AddWithValue("run_id", run.Id);
            statusCommand.Parameters.AddWithValue("character_id", status.CharacterId);
            statusCommand.Parameters.AddWithValue("source_character_id", status.SourceCharacterId);
            statusCommand.Parameters.AddWithValue("status", status.Status);
            statusCommand.Parameters.AddWithValue("moves_processed", status.MovesProcessed);
            statusCommand.Parameters.AddWithValue("error", (object?)status.Error ?? DBNull.Value);
            await statusCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IngestionRun?> GetByIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, started_at, completed_at, status, characters_processed, moves_processed, errors
            FROM ingestion_runs
            WHERE id = @id
            LIMIT 1;
            """;

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var run = MapRun(reader);
        await reader.CloseAsync();
        run.CharacterStatuses.AddRange(await GetCharacterStatusesAsync(connection, run.Id, cancellationToken));
        return run;
    }

    private static IngestionRun MapRun(NpgsqlDataReader reader)
    {
        var completedAtOrdinal = reader.GetOrdinal("completed_at");
        var errorsJson = reader.GetString(reader.GetOrdinal("errors"));

        return new IngestionRun
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            StartedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("started_at")),
            CompletedAt = reader.IsDBNull(completedAtOrdinal) ? null : reader.GetFieldValue<DateTimeOffset>(completedAtOrdinal),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CharactersProcessed = reader.GetInt32(reader.GetOrdinal("characters_processed")),
            MovesProcessed = reader.GetInt32(reader.GetOrdinal("moves_processed")),
            Errors = JsonSerializer.Deserialize<List<string>>(errorsJson) ?? []
        };
    }

    private static async Task<IReadOnlyList<IngestionRunCharacterStatus>> GetCharacterStatusesAsync(
        NpgsqlConnection connection,
        string runId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT character_id, source_character_id, status, moves_processed, error
            FROM ingestion_run_character_statuses
            WHERE run_id = @run_id
            ORDER BY character_id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("run_id", runId);

        var statuses = new List<IngestionRunCharacterStatus>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var errorOrdinal = reader.GetOrdinal("error");
            statuses.Add(new IngestionRunCharacterStatus
            {
                CharacterId = reader.GetString(reader.GetOrdinal("character_id")),
                SourceCharacterId = reader.GetInt32(reader.GetOrdinal("source_character_id")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                MovesProcessed = reader.GetInt32(reader.GetOrdinal("moves_processed")),
                Error = reader.IsDBNull(errorOrdinal) ? null : reader.GetString(errorOrdinal)
            });
        }

        return statuses;
    }
}
