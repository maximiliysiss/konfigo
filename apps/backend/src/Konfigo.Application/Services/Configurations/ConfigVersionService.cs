using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Extensions;
using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.Configurations.Extensions;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Application.Services.Configurations.Options;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Medallion.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateVersionRequest = Konfigo.Application.Services.Configurations.Models.UpdateVersionRequest;

namespace Konfigo.Application.Services.Configurations;

internal sealed class ConfigVersionService : IConfigVersionService
{
    private readonly IConfigVersionsRepository _repository;

    private readonly IDateTimeProvider _dateTimeProvider;

    private readonly ILogger<ConfigVersionService> _logger;

    private readonly IDistributedLockProvider _distributedLockProvider;

    private readonly ConfigVersionServiceOptions _options;

    public ConfigVersionService(
        IConfigVersionsRepository repository,
        IDateTimeProvider dateTimeProvider,
        ILogger<ConfigVersionService> logger,
        IDistributedLockProvider distributedLockProvider,
        IOptions<ConfigVersionServiceOptions> options)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _distributedLockProvider = distributedLockProvider;
        _options = options.Value;
    }

    public async Task<ConfigVersion> CreateAsync(CreateVersionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogConfigVersionCreateStarted(request.ServiceId, request.VersionLabel);

        var version = new ConfigVersion
        {
            ServiceId = request.ServiceId,
            VersionLabel = request.VersionLabel,
            Description = request.Description,
            CreatedAt = _dateTimeProvider.GetNow(),
            Id = VersionId.New(),
        };

        version = await _repository.AddAsync(version, cancellationToken);

        _logger.LogConfigVersionCreated(request.ServiceId, version.Id, version.VersionLabel);

        return version;
    }

    public async Task<ConfigVersion?> UpdateAsync(UpdateVersionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogConfigVersionUpdateStarted(request.ServiceId, request.VersionId, request.VersionLabel);

        var version = await _repository
            .GetAsync(SearchVersionRequest.Create(serviceId: request.ServiceId, ids: [request.VersionId]), cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            _logger.LogConfigVersionNotFound(request.ServiceId, request.VersionId);
            return null;
        }

        var updateVersionRequest = new Domain.ValueType.UpdateVersionRequest(
            VersionLabel: request.VersionLabel,
            Description: request.Description);

        version.Update(updateVersionRequest, _dateTimeProvider.GetNow());

        await _repository.UpdateAsync(version, cancellationToken);

        _logger.LogConfigVersionUpdated(request.ServiceId, version.Id, version.VersionLabel);

        return version;
    }

    public async Task<ConfigVersion> GenerateAsync(GenerateVersionRequest request, CancellationToken cancellationToken)
    {
        _logger.LogConfigVersionGenerateStarted(request.ServiceId, request.VersionLabel, request.Entries.Length);

        await using var _ = await _distributedLockProvider.AcquireLockAsync(
            name: (request.ServiceId, request.VersionLabel).AsKey(),
            timeout: _options.LockTimeout,
            cancellationToken: cancellationToken);

        var searchExistingVersionRequest = SearchVersionRequest.Create(
            serviceId: request.ServiceId,
            label: request.VersionLabel,
            include: [EEntityType.Entry],
            asTracking: false);

        var existingVersion = await _repository
            .GetAsync(searchExistingVersionRequest, cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingVersion is not null)
        {
            _logger.LogConfigVersionAlreadyExists(
                request.ServiceId,
                existingVersion.Id,
                request.VersionLabel);

            return existingVersion;
        }

        var searchVersionRequest = SearchVersionRequest.Create(
            serviceId: request.ServiceId,
            limit: 1,
            include: [EEntityType.Entry],
            asTracking: false);

        var previousVersion = await _repository
            .GetAsync(searchVersionRequest, cancellationToken)
            .SingleOrDefaultAsync(cancellationToken);

        var existingEntries = previousVersion?.ConfigEntries.ToDictionary(c => c.Key) ?? [];

        var now = _dateTimeProvider.GetNow();
        var versionId = VersionId.New();

        var configVersion = new ConfigVersion
        {
            Id = versionId,
            ServiceId = request.ServiceId,
            VersionLabel = request.VersionLabel,
            CreatedAt = now,
            Description = request.Description,
            ConfigEntries = request.Entries
                .Select(Map)
                .ToArray(),
        };

        configVersion = await _repository.AddAsync(configVersion, cancellationToken);

        _logger.LogConfigVersionGenerated(
            request.ServiceId,
            configVersion.Id,
            configVersion.VersionLabel,
            configVersion.ConfigEntries.Count);

        return configVersion;

        ConfigEntry Map(GenerateVersionRequest.EntryRequest entry)
        {
            var existingEntry = existingEntries.GetValueOrDefault(entry.Key);

            var value = existingEntry is not null && existingEntry.ValueType == entry.ValueType
                ? existingEntry.RawValue
                : entry.RawValue;

            return new ConfigEntry
            {
                Id = EntryId.New(),
                Name = entry.Name,
                RawValue = value,
                Generation = 1,
                Description = entry.Description,
                EnumDefinition = entry.EnumDefinition,
                GroupName = entry.GroupName,
                ConfigVersionId = versionId,
                ValueType = entry.ValueType,
                Key = entry.Key,
                CreatedAt = now,
                GroupDescription = entry.GroupDescription,
            };
        }
    }
}
