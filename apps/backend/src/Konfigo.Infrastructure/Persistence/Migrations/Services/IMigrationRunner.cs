using System.Threading;
using System.Threading.Tasks;

namespace Konfigo.Infrastructure.Persistence.Migrations.Services;

internal interface IMigrationRunner
{
    Task MigrateUpAsync(CancellationToken cancellationToken);
}
