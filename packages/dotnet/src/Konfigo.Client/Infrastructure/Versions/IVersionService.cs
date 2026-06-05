using System.Threading;
using System.Threading.Tasks;
using Konfigo.Client.Entities;

namespace Konfigo.Client.Infrastructure.Versions;

internal interface IVersionService
{
    Task<VersionId> CreateAsync(CancellationToken cancellationToken);
}
