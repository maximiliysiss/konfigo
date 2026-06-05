using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Microsoft.EntityFrameworkCore;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class ConfigVersionsRepository(AppDbContext context) : IConfigVersionsRepository
{
    public IAsyncEnumerable<ConfigVersion> GetAsync(SearchVersionRequest request, CancellationToken cancellationToken)
    {
        IQueryable<ConfigVersion> query = context.ConfigVersions;

        foreach (var type in request.Include)
        {
            query = type switch
            {
                EEntityType.Entry => query.Include(x => x.ConfigEntries),
                _ => query,
            };
        }

        query = query.Where(x => x.ServiceId == request.ServiceId);

        if (request.Ids is [_, ..])
        {
            query = query.Where(x => request.Ids.Contains(x.Id));
        }

        if (!string.IsNullOrEmpty(request.Label))
        {
            query = query.Where(x => x.VersionLabel == request.Label);
        }

        query = query
            .OrderByDescending(x => x.CreatedAt)
            .Take(request.Limit);

        if (request.Include is not [])
        {
            query = query.AsSplitQuery();
        }

        if (!request.AsTracking)
        {
            query = query.AsNoTracking();
        }

        return query.AsAsyncEnumerable();
    }

    public async Task<ConfigVersion> AddAsync(ConfigVersion version, CancellationToken cancellationToken)
    {
        await context.ConfigVersions.AddAsync(version, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return version;
    }

    public Task UpdateAsync(ConfigVersion version, CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
