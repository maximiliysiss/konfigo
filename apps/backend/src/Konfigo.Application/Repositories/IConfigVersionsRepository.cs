using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Repositories;

public interface IConfigVersionsRepository
{
    IAsyncEnumerable<ConfigVersion> GetAsync(SearchVersionRequest request, CancellationToken cancellationToken);
    Task<ConfigVersion> AddAsync(ConfigVersion version, CancellationToken cancellationToken);
    Task UpdateAsync(ConfigVersion version, CancellationToken cancellationToken);
}
