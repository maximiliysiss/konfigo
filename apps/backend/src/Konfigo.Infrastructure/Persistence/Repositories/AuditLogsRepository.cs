using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogsRepository(AppDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog[] auditLog, CancellationToken cancellationToken)
    {
        await context.AuditLogs.AddRangeAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public IAsyncEnumerable<AuditLog> GetAsync(SearchAuditLogRequest request, CancellationToken cancellationToken)
    {
        IQueryable<AuditLog> query = context.AuditLogs;

        query = query.Where(c => c.ServiceId == request.ServiceId);

        query = query
            .Where(x => x.Num < request.Cursor.Num)
            .OrderByDescending(x => x.Num)
            .Take(request.PageSize);

        if (!request.AsTracking)
        {
            query = query.AsNoTracking();
        }

        return query.AsAsyncEnumerable();
    }
}
