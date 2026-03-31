using System.Data;
using Npgsql;

namespace FrameData.Infrastructure.Persistence;

public sealed class UnitOfWork : IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    public UnitOfWork(DbConnectionFactory factory)
    {
        _connection = factory.CreateOpenConnection();
        _transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
    }

    public NpgsqlConnection Connection => _connection;
    public NpgsqlTransaction Transaction => _transaction;

    public void Commit() => _transaction.Commit();

    public void Dispose()
    {
        _transaction.Dispose();
        _connection.Dispose();
    }
}
