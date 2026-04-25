using Npgsql;

namespace FrameData.Infrastructure.Persistence;

public sealed class SchemaBootstrapper
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly string? _schemaPath;

    public SchemaBootstrapper(DbConnectionFactory connectionFactory, string? schemaPath = null)
    {
        _connectionFactory = connectionFactory;
        _schemaPath = schemaPath;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var schemaPath = ResolveSchemaPath(_schemaPath);
        var sql = await File.ReadAllTextAsync(schemaPath, cancellationToken);

        await using var connection = _connectionFactory.CreateOpenConnection();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ResolveSchemaPath(string? configuredSchemaPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredSchemaPath) && File.Exists(configuredSchemaPath))
        {
            return configuredSchemaPath;
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Persistence", "Migrations", "0001_Initial.sql"),
            Path.Combine(AppContext.BaseDirectory, "FrameData.Infrastructure", "Persistence", "Migrations", "0001_Initial.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "FrameData.Infrastructure", "Persistence", "Migrations", "0001_Initial.sql"),
            Path.Combine(Directory.GetCurrentDirectory(), "Persistence", "Migrations", "0001_Initial.sql")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Could not locate FrameData.Infrastructure schema bootstrap SQL.");
    }
}
