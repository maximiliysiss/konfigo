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

        var auditLog = new AuditLog
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

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        _logger.LogVersionCreateAuditCompleted(request.ServiceId);

        transactionScope.Complete();

        return configVersion;
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
            var auditLog = new AuditLog
            {
                Entry = new VersionUpdatedEntry(configVersion.Id, request.VersionLabel, request.Description),
                ServiceId = request.ServiceId,
                UserId = request.UpdatedBy,
                Id = LogId.New(),
                CreatedAt = configVersion.CreatedAt,
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _logger.LogVersionUpdateAuditCompleted(request.ServiceId);
        }

        transactionScope.Complete();

        return configVersion;
    }

    public async Task<ConfigVersion> GenerateAsync(GenerateVersionRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configVersion = await _service.GenerateAsync(request, cancellationToken);

        var versionLog = new AuditLog
        {
            Entry = new VersionCreatedEntry(
                Id: configVersion.Id,
                VersionLabel: configVersion.VersionLabel,
                Description: configVersion.Description),
            ServiceId = configVersion.ServiceId,
            UserId = null,
            Id = LogId.New(),
            CreatedAt = configVersion.CreatedAt,
        };

        var entryLogs = configVersion.ConfigEntries
            .Select(Map)
            .ToArray();

        await _auditLogRepository.AddAsync([versionLog, .. entryLogs], cancellationToken);
        _logger.LogVersionGenerateAuditCompleted(request.ServiceId, entryLogs.Length + 1);

        transactionScope.Complete();

        return configVersion;

        AuditLog Map(ConfigEntry entry)
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
    }
}
