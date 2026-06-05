using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Konfigo.Infrastructure.Persistence.Factory;

internal interface IConnectionFactory
{
    string GetConnectionString();
    Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
