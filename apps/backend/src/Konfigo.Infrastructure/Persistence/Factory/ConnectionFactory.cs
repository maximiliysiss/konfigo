using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Konfigo.Infrastructure.Persistence.Factory;

internal sealed class ConnectionFactory : IConnectionFactory, Publo.Postgres.Infrastructure.Database.IConnectionFactory
{
    private readonly DbDataSource _source;

    public ConnectionFactory(DbDataSource source) => _source = source;

    public string GetConnectionString() => _source.ConnectionString;

    public Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken) => Task.FromResult(_source.CreateConnection());
}
