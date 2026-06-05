using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;

namespace Konfigo.Application.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken) => AddAsync([auditLog], cancellationToken);
    Task AddAsync(AuditLog[] auditLog, CancellationToken cancellationToken);
    IAsyncEnumerable<AuditLog> GetAsync(SearchAuditLogRequest request, CancellationToken cancellationToken);
}
