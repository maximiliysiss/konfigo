using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Authorization;
using Konfigo.Controllers.Converters;
using Konfigo.Controllers.Models.Audit;
using Konfigo.Controllers.Models.Shared;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Konfigo.Controllers;

[Authorize(Policy = AuthorizationPolicyNames.CanChange)]
[ApiController]
public sealed class AuditLogsController(IAuditLogRepository auditLogRepository, ILogger<AuditLogsController> logger) : ControllerBase
{
    [HttpPost("api/audit/{serviceId:guid}/search")]
    public async Task<PageResponse<AuditLog>> Handle(
        [FromRoute] Guid serviceId,
        [FromBody] SearchAuditRequest contract,
        CancellationToken cancellationToken)
    {
        var id = new ServiceId(serviceId);

        logger.LogAuditLogSearchStarted(id, contract.PageSize);

        var pageToken = contract.PageToken.AsPageToken(SearchAuditLogRequest.PageToken.Empty);

        var searchAuditLogRequest = SearchAuditLogRequest.Create(
            serviceId: id,
            pageSize: contract.PageSize,
            cursor: pageToken,
            asTracking: false);

        var auditLogs = await auditLogRepository
            .GetAsync(searchAuditLogRequest, cancellationToken)
            .ToArrayAsync(cancellationToken);

        logger.LogAuditLogSearchCompleted(id, contract.PageSize, auditLogs.Length);

        var nextPageToken = auditLogs.Length < contract.PageSize
            ? string.Empty
            : new SearchAuditLogRequest.PageToken(auditLogs[^1].Num).AsBase64();

        return new PageResponse<AuditLog>
        {
            Entities = auditLogs,
            NextPageToken = nextPageToken,
        };
    }
}
