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
using UpdateEntryRequest = Konfigo.Application.Services.Configurations.Models.UpdateEntryRequest;

namespace Konfigo.Application.Services.Configurations.Audit;

internal sealed class AuditConfigEntryService : IConfigEntryService
{
    private readonly IConfigEntryService _service;

    private readonly IAuditLogRepository _auditLogRepository;

    private readonly ILogger<AuditConfigEntryService> _logger;

    public AuditConfigEntryService(
        IConfigEntryService service,
        IAuditLogRepository auditLogRepository,
        ILogger<AuditConfigEntryService>? logger = null)
    {
        _service = service;
        _auditLogRepository = auditLogRepository;
        _logger = logger ?? NullLogger<AuditConfigEntryService>.Instance;
    }

    public async Task<ConfigEntry> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configEntry = await _service.CreateAsync(request, cancellationToken);

        var auditLog = new AuditLog
        {
            Entry = new EntryCreatedEntry(
                Id: configEntry.Id,
                RawValue: request.RawValue,
                EnumDefinition: request.EnumDefinition,
                Description: request.Description,
                GroupName: request.GroupName,
                GroupDescription: request.GroupDescription),
            ServiceId = request.ServiceId,
            UserId = request.CreatedBy,
            Id = LogId.New(),
            CreatedAt = configEntry.CreatedAt,
        };

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        _logger.LogEntryCreateAuditCompleted(request.ServiceId);

        transactionScope.Complete();

        return configEntry;
    }

    public async Task<ConfigEntry?> UpdateAsync(UpdateEntryRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configEntry = await _service.UpdateAsync(request, cancellationToken);

        if (configEntry is not null)
        {
            var auditLog = new AuditLog
            {
                Entry = new EntryUpdatedEntry(
                    Id: configEntry.Id,
                    RawValue: request.RawValue,
                    EnumDefinition: request.EnumDefinition,
                    Description: request.Description,
                    GroupName: request.GroupName,
                    GroupDescription: request.GroupDescription),
                ServiceId = request.ServiceId,
                UserId = request.UpdatedBy,
                Id = LogId.New(),
                CreatedAt = configEntry.UpdatedAt ?? configEntry.CreatedAt,
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _logger.LogEntryUpdateAuditCompleted(request.ServiceId);
        }

        transactionScope.Complete();

        return configEntry;
    }

    public async Task<ConfigEntry[]> SetAsync(SetEntryRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configEntries = await _service.SetAsync(request, cancellationToken);

        var auditLogs = configEntries
            .Select(Map)
            .ToArray();

        await _auditLogRepository.AddAsync(auditLogs, cancellationToken);
        _logger.LogEntrySetAuditCompleted(request.ServiceId, auditLogs.Length);

        transactionScope.Complete();

        return configEntries;

        AuditLog Map(ConfigEntry entry)
        {
            return new AuditLog
            {
                ServiceId = request.ServiceId,
                UserId = request.UpdatedBy.Id,
                Id = LogId.New(),
                CreatedAt = entry.UpdatedAt ?? entry.CreatedAt,
                Entry = new EntrySetEntry(entry.Id, entry.RawValue),
            };
        }
    }

    public async Task<ConfigEntry?> DeleteAsync(DeleteEntryRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configEntry = await _service.DeleteAsync(request, cancellationToken);

        if (configEntry is not null)
        {
            var auditLog = new AuditLog
            {
                Entry = new EntryDeletedEntry(
                    Id: configEntry.Id,
                    RawValue: configEntry.RawValue,
                    EnumDefinition: configEntry.EnumDefinition,
                    Description: configEntry.Description,
                    GroupName: configEntry.GroupName,
                    GroupDescription: configEntry.GroupDescription),
                ServiceId = request.ServiceId,
                UserId = request.DeletedBy,
                Id = LogId.New(),
                CreatedAt = configEntry.UpdatedAt ?? configEntry.CreatedAt,
            };

            await _auditLogRepository.AddAsync(auditLog, cancellationToken);
            _logger.LogEntryDeleteAuditCompleted(request.ServiceId);
        }

        transactionScope.Complete();

        return configEntry;
    }
}
