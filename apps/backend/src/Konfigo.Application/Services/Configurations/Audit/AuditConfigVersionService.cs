using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Konfigo.Application.Extensions;
using Konfigo.Application.Repositories;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UpdateVersionRequest = Konfigo.Application.Services.Configurations.Models.UpdateVersionRequest;

namespace Konfigo.Application.Services.Configurations.Audit;

internal sealed class AuditConfigVersionService : IConfigVersionService
{
    private readonly IConfigVersionService _service;

    private readonly IAuditLogRepository _auditLogRepository;

    private readonly ILogger<AuditConfigVersionService> _logger;

    public AuditConfigVersionService(
        IConfigVersionService service,
        IAuditLogRepository auditLogRepository,
        ILogger<AuditConfigVersionService>? logger = null)
    {
        _service = service;
        _auditLogRepository = auditLogRepository;
        _logger = logger ?? NullLogger<AuditConfigVersionService>.Instance;
    }

    public async Task<ConfigVersion> CreateAsync(CreateVersionRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configVersion = await _service.CreateAsync(request, cancellationToken);
        await _auditLogRepository.AddAsync(MapAudit(), cancellationToken);

        _logger.LogVersionCreateAuditCompleted(request.ServiceId);

        transactionScope.Complete();

        return configVersion;

        AuditLog MapAudit()
        {
            return new AuditLog
            {
                Entry = new VersionCreatedEntry(
                    Id: configVersion.Id,
                    VersionLabel: request.VersionLabel,
                    Description: request.Description),
                ServiceId = request.ServiceId,
                UserId = request.CreatedBy,
                Id = LogId.New(),
                CreatedAt = configVersion.CreatedAt,
            };
        }
    }

    public async Task<ConfigVersion?> UpdateAsync(UpdateVersionRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configVersion = await _service.UpdateAsync(request, cancellationToken);

        if (configVersion is not null)
        {
            await _auditLogRepository.AddAsync(MapAudit(), cancellationToken);
            _logger.LogVersionUpdateAuditCompleted(request.ServiceId);
        }

        transactionScope.Complete();

        return configVersion;

        AuditLog MapAudit()
        {
            return new AuditLog
            {
                Entry = new VersionUpdatedEntry(configVersion.Id, request.VersionLabel, request.Description),
                ServiceId = request.ServiceId,
                UserId = request.UpdatedBy,
                Id = LogId.New(),
                CreatedAt = configVersion.CreatedAt,
            };
        }
    }

    public async Task<GenerateResult> GenerateAsync(GenerateVersionRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configVersion = await _service.GenerateAsync(request, cancellationToken);

        if (configVersion is GenerateResult.New)
        {
            var entryLogs = configVersion.Version.ConfigEntries
                .Select(MapEntryAudit)
                .ToArray();

            await _auditLogRepository.AddAsync([MapVersionAudit(), .. entryLogs], cancellationToken);

            _logger.LogVersionGenerateAuditCompleted(request.ServiceId, entryLogs.Length + 1);
        }

        transactionScope.Complete();

        return configVersion;

        AuditLog MapEntryAudit(ConfigEntry entry)
        {
            return new AuditLog
            {
                ServiceId = request.ServiceId,
                UserId = null,
                CreatedAt = entry.CreatedAt,
                Id = LogId.New(),
                Entry = new EntryCreatedEntry(
                    Id: entry.Id,
                    RawValue: entry.RawValue,
                    EnumDefinition: entry.EnumDefinition,
                    Description: entry.Description,
                    GroupName: entry.GroupName,
                    GroupDescription: entry.GroupDescription),
            };
        }

        AuditLog MapVersionAudit()
        {
            return new AuditLog
            {
                Entry = new VersionCreatedEntry(
                    Id: configVersion.Version.Id,
                    VersionLabel: configVersion.Version.VersionLabel,
                    Description: configVersion.Version.Description),
                ServiceId = configVersion.Version.ServiceId,
                UserId = null,
                Id = LogId.New(),
                CreatedAt = configVersion.Version.CreatedAt,
            };
        }
    }
}
