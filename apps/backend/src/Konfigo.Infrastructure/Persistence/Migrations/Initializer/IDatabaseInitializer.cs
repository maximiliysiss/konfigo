using System.Threading;
using System.Threading.Tasks;

namespace Konfigo.Infrastructure.Persistence.Migrations.Initializer;

internal interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
