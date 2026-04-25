using Npgsql;

namespace FrameData.Infrastructure.Persistence;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = NormalizeConnectionString(connectionString);
    }

    public NpgsqlConnection CreateOpenConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!connectionString.Contains("GSS Encryption Mode", StringComparison.OrdinalIgnoreCase))
        {
            builder["GSS Encryption Mode"] = "Disable";
        }

        return builder.ConnectionString;
    }
}
