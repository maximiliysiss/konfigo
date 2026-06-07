using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class ConfigEntryRepository(AppDbContext context) : IConfigEntryRepository
{
    public IAsyncEnumerable<ConfigEntry> GetAsync(SearchEntryRequest request, CancellationToken cancellationToken)
    {
        IQueryable<ConfigEntry> query = context.ConfigEntries;

        query = query.Where(x => x.ConfigVersionId == request.VersionId && x.ConfigVersion.ServiceId == request.ServiceId);

        if (request.Ids is [_, ..])
        {
            query = query.Where(x => request.Ids.Contains(x.Id));
        }

        if (request.From is not null)
        {
            query = query.Where(x => (x.UpdatedAt ?? x.CreatedAt) >= request.From);
        }

        if (!request.AsTracking)
        {
            query = query.AsNoTracking();
        }

        return query.AsAsyncEnumerable();
    }

    public async Task<ConfigEntry> AddAsync(ConfigEntry entry, CancellationToken cancellationToken)
    {
        await context.ConfigEntries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return entry;
    }

    public Task UpdateAsync(ConfigEntry[] entry, CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(ConfigEntry entry, CancellationToken cancellationToken)
    {
        context.ConfigEntries.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
