using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Repositories;

public interface IConfigEntryRepository
{
    IAsyncEnumerable<ConfigEntry> GetAsync(SearchEntryRequest request, CancellationToken cancellationToken);
    Task<ConfigEntry> AddAsync(ConfigEntry entry, CancellationToken cancellationToken);
    Task UpdateAsync(ConfigEntry[] entry, CancellationToken cancellationToken);
    Task DeleteAsync(ConfigEntry entry, CancellationToken cancellationToken);
}
