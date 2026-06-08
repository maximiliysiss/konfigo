using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Extensions;
using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Application.Services.Configurations.Options;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateEntryRequest = Konfigo.Application.Services.Configurations.Models.UpdateEntryRequest;

namespace Konfigo.Application.Services.Configurations;

internal sealed class ConfigEntryService : IConfigEntryService
{
    private readonly IConfigEntryRepository _configEntryRepository;
    private readonly IApplicationsRepository _applicationsRepository;

    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly ILogger<ConfigEntryService> _logger;

    private readonly IDistributedLockProvider _distributedLockFactory;

    private readonly ConfigEntryServiceOptions _options;

    public ConfigEntryService(
        IConfigEntryRepository configEntryRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ConfigEntryService> logger,
        IDistributedLockProvider distributedLockFactory,
        IOptions<ConfigEntryServiceOptions> options,
        IApplicationsRepository applicationsRepository)
    {
        _configEntryRepository = configEntryRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _distributedLockFactory = distributedLockFactory;
        _options = options.Value;
        _applicationsRepository = applicationsRepository;
    }

    public async Task<ConfigEntry> CreateAsync(CreateEntryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogConfigEntryCreateStarted(request.ServiceId, request.VersionId);

        var configEntry = new ConfigEntry
        {
            Name = request.Name,
            Description = request.Description,
            GroupName = request.GroupName,
            GroupDescription = request.GroupDescription,
            CreatedAt = _dateTimeProvider.GetNow(),
            Key = request.Key,
            RawValue = request.RawValue,
            ValueType = request.ValueType,
            EnumDefinition = request.EnumDefinition,
            Id = EntryId.New(),
            ConfigVersionId = request.VersionId,
            Generation = 1,
        };

        await _configEntryRepository.AddAsync(configEntry, cancellationToken);

        _logger.LogConfigEntryCreated(request.ServiceId, request.VersionId, configEntry.Id);

        return configEntry;
    }

    public async Task<ConfigEntry?> UpdateAsync(UpdateEntryRequest request, CancellationToken cancellationToken)
    {
        await using var _ = await _distributedLockFactory.TryAcquireOrThrowAsync(
            key: (request.ServiceId, request.VersionId).AsKey(),
            timeout: _options.LockTimeout,
            cancellationToken: cancellationToken);

        _logger.LogConfigEntryUpdateStarted(request.ServiceId, request.VersionId, request.Id);

        var entry = await _configEntryRepository
            .GetAsync(SearchEntryRequest.Create(request.ServiceId, request.VersionId, ids: [request.Id]), cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            _logger.LogConfigEntryNotFound(request.ServiceId, request.VersionId, request.Id);
            return null;
        }

        var updateEntryRequest = new Domain.ValueType.UpdateEntryRequest(
            RawValue: request.RawValue,
            EnumDefinition: request.EnumDefinition,
            Description: request.Description,
            GroupName: request.GroupName,
            GroupDescription: request.GroupDescription,
            Generation: request.Generation);

        entry.Update(updateEntryRequest, _dateTimeProvider.GetNow());

        await _configEntryRepository.UpdateAsync([entry], cancellationToken);

        _logger.LogConfigEntryUpdated(request.ServiceId, request.VersionId, entry.Id);

        return entry;
    }

    public async Task<ConfigEntry[]> SetAsync(SetEntryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogConfigEntrySetStarted(request.ServiceId, request.VersionId, request.Requests.Length);

        var service = await _applicationsRepository
            .GetAsync(SearchServiceRequest.Create(ids: [request.ServiceId]), cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            _logger.LogApplicationServiceNotFound(request.ServiceId);
            return [];
        }

        if (request.UpdatedBy is not null && !service.Members.Contains(request.UpdatedBy.Value))
        {
            _logger.LogAccessDenied(request.ServiceId, request.UpdatedBy);
            throw new UnauthorizedAccessException("User is not a member of the service");
        }

        await using var _ = await _distributedLockFactory.TryAcquireOrThrowAsync(
            key: (request.ServiceId, request.VersionId).AsKey(),
            timeout: _options.LockTimeout,
            cancellationToken: cancellationToken);

        var originalRequests = request.Requests
            .DistinctBy(c => c.Id)
            .ToArray();

        var searchEntryRequest = SearchEntryRequest.Create(
            serviceId: request.ServiceId,
            versionId: request.VersionId,
            ids: originalRequests.Select(c => c.Id).ToArray());

        var entries = await _configEntryRepository
            .GetAsync(searchEntryRequest, cancellationToken)
            .ToDictionaryAsync(c => c.Id, cancellationToken: cancellationToken);

        if (entries.Count != originalRequests.Length)
        {
            _logger.LogConfigEntriesNotFound(
                request.ServiceId,
                request.VersionId,
                requestedCount: originalRequests.Length,
                foundCount: entries.Count);

            return [];
        }

        var now = _dateTimeProvider.GetNow();

        foreach (var setRequest in originalRequests)
        {
            entries[setRequest.Id].Set(setRequest.RawValue, now, setRequest.Generation);
        }

        ConfigEntry[] configEntries = [.. entries.Values];

        await _configEntryRepository.UpdateAsync(configEntries, cancellationToken);

        _logger.LogConfigEntrySetCompleted(request.ServiceId, request.VersionId, configEntries.Length);

        return configEntries;
    }

    public async Task<ConfigEntry?> DeleteAsync(DeleteEntryRequest request, CancellationToken cancellationToken)
    {
        await using var _ = await _distributedLockFactory.TryAcquireOrThrowAsync(
            key: (request.ServiceId, request.VersionId).AsKey(),
            timeout: _options.LockTimeout,
            cancellationToken: cancellationToken);

        _logger.LogConfigEntryDeleteStarted(request.ServiceId, request.VersionId, request.Id);

        var entry = await _configEntryRepository
            .GetAsync(SearchEntryRequest.Create(request.ServiceId, request.VersionId, ids: [request.Id]), cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (entry is null)
        {
            _logger.LogConfigEntryNotFound(request.ServiceId, request.VersionId, request.Id);
            return null;
        }

        await _configEntryRepository.DeleteAsync(entry, cancellationToken);

        _logger.LogConfigEntryDeleted(request.ServiceId, request.VersionId, entry.Id);

        return entry;
    }
}
