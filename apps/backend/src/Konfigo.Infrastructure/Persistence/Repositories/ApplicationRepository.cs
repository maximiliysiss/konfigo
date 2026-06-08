using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class ApplicationRepository(AppDbContext context) : IApplicationsRepository
{
    public IAsyncEnumerable<ApplicationService> GetAsync(SearchServiceRequest request, CancellationToken cancellationToken)
    {
        IQueryable<ApplicationService> query = context.ApplicationServices;

        if (request.Ids is [_, ..])
            query = query.Where(x => request.Ids.Contains(x.Id));

        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(x => x.Name.Contains(request.Name));

        if (request.Member is not null)
            query = query.Where(x => x.Members.Contains(request.Member.Value));

        query = query
            .Where(x => x.Num < request.Cursor.Num)
            .OrderByDescending(x => x.Num)
            .Take(request.PageSize);

        if (!request.AsTracking)
            query = query.AsNoTracking();

        return query.AsAsyncEnumerable();
    }

    public async Task<ApplicationService> AddAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        await context.ApplicationServices.AddAsync(service, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return service;
    }

    public Task UpdateAsync(ApplicationService service, CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);

    public Task DeleteAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        context.ApplicationServices.Remove(service);
        return context.SaveChangesAsync(cancellationToken);
    }
}
