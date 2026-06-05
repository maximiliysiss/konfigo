using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Konfigo.Application.Extensions;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Application.Services.Notifications;
using Konfigo.Application.Services.Notifications.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UpdateEntryRequest = Konfigo.Application.Services.Configurations.Models.UpdateEntryRequest;

namespace Konfigo.Application.Services.Configurations.Tracking;

internal sealed class TrackingConfigEntryService : IConfigEntryService
{
    private readonly IConfigEntryService _service;

    private readonly IEnumerable<IConfigChangeNotifier> _notifiers;

    private readonly ILogger<TrackingConfigEntryService> _logger;

    public TrackingConfigEntryService(
        IConfigEntryService service,
        IEnumerable<IConfigChangeNotifier> notifiers,
        ILogger<TrackingConfigEntryService>? logger = null)
    {
        _service = service;
        _notifiers = notifiers;
        _logger = logger ?? NullLogger<TrackingConfigEntryService>.Instance;
    }

    public async Task<ConfigEntry> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configEntry = await _service.CreateAsync(request, cancellationToken);
        await NotifyAsync(request.ServiceId, request.VersionId, [configEntry], cancellationToken);

        transactionScope.Complete();

        return configEntry;
    }

    public Task<ConfigEntry?> UpdateAsync(UpdateEntryRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            serviceId: request.ServiceId,
            versionId: request.VersionId,
            action: () => _service.UpdateAsync(request, cancellationToken),
            cancellationToken: cancellationToken);
    }

    public async Task<ConfigEntry[]> SetAsync(SetEntryRequest request, CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configEntries = await _service.SetAsync(request, cancellationToken);
        await NotifyAsync(request.ServiceId, request.VersionId, configEntries, cancellationToken);

        transactionScope.Complete();

        return configEntries;
    }

    public Task<ConfigEntry?> DeleteAsync(DeleteEntryRequest request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            serviceId: request.ServiceId,
            versionId: request.VersionId,
            action: () => _service.DeleteAsync(request, cancellationToken),
            cancellationToken: cancellationToken);
    }

    private async Task<ConfigEntry?> ExecuteAsync(
        ServiceId serviceId,
        VersionId versionId,
        Func<Task<ConfigEntry?>> action,
        CancellationToken cancellationToken)
    {
        using var transactionScope = new TransactionScope(
            scopeOption: TransactionScopeOption.Required,
            transactionOptions: new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
            asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled);

        var configEntry = await action();

        if (configEntry is not null)
        {
            await NotifyAsync(serviceId, versionId, [configEntry], cancellationToken);
        }

        transactionScope.Complete();

        return configEntry;
    }

    private async Task NotifyAsync(ServiceId serviceId, VersionId versionId, ConfigEntry[] configEntry, CancellationToken cancellationToken)
    {
        if (configEntry is [])
        {
            _logger.LogNotificationSkippedEmptyEntryBatch(serviceId, versionId);
            return;
        }

        var requests = configEntry
            .Select(Map)
            .ToArray();

        var notificationRequest = new NotificationRequest(
            ServiceId: serviceId,
            VersionId: versionId,
            Requests: requests);

        var notifiers = _notifiers.ToArray();

        _logger.LogNotificationStarted(serviceId, versionId, requests.Length);

        foreach (var notifier in notifiers)
        {
            await notifier.HandleAsync(notificationRequest, cancellationToken);
        }

        _logger.LogNotificationCompleted(serviceId, versionId, requests.Length);

        return;

        static NotificationRequest.Request Map(ConfigEntry entry)
        {
            var timestamp = entry.UpdatedAt ?? entry.CreatedAt;

            return new NotificationRequest.Request(
                EntryId: entry.Id,
                Key: entry.Key,
                RawValue: entry.RawValue,
                Generation: entry.Generation,
                Timestamp: timestamp.UtcDateTime);
        }
    }
}
