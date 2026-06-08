using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Konfigo.Application.Infrastructure.DateTime;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Application.Services.Updater;
using Konfigo.Application.Services.Updater.Models;
using Konfigo.Client.Grpc;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Extensions;
using Konfigo.Grpc.Converters;
using Microsoft.Extensions.Logging;
using CreateVersionRequest = Konfigo.Client.Grpc.CreateVersionRequest;

namespace Konfigo.Grpc;

public class RealtimeConfigGrpcService : Client.Grpc.RealtimeConfigGrpcService.RealtimeConfigGrpcServiceBase
{
    private readonly IUpdaterService _updaterService;

    private readonly IConfigVersionsRepository _configVersionsRepository;
    private readonly IConfigEntryRepository _configEntryRepository;

    private readonly IConfigVersionService _configVersionService;

    private readonly ILogger<RealtimeConfigGrpcService> _logger;

    private readonly IDateTimeProvider _dateTimeProvider;

    public RealtimeConfigGrpcService(
        IUpdaterService updaterService,
        IConfigVersionsRepository configVersionsRepository,
        IConfigEntryRepository configEntryRepository,
        IConfigVersionService configVersionService,
        ILogger<RealtimeConfigGrpcService> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _updaterService = updaterService;
        _configVersionsRepository = configVersionsRepository;
        _configEntryRepository = configEntryRepository;
        _configVersionService = configVersionService;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public override async Task<GetConfigResponse> GetConfig(GetConfigRequest request, ServerCallContext context)
    {
        var serviceId = new ServiceId(Guid.Parse(request.ServiceId));

        _logger.LogGrpcGetConfigStarted(serviceId, request.Version);

        var searchVersionRequest = SearchVersionRequest.Create(
            serviceId: serviceId,
            label: request.Version);

        var version = await _configVersionsRepository
            .GetAsync(searchVersionRequest, context.CancellationToken)
            .SingleOrDefaultAsync(context.CancellationToken);

        if (version is null)
        {
            _logger.LogGrpcGetConfigMissingVersion(serviceId, request.Version);

            return new GetConfigResponse
            {
                ServiceId = request.ServiceId,
                VersionId = string.Empty,
                Entries = { },
            };
        }

        var searchEntryRequest = SearchEntryRequest.Create(
            serviceId: serviceId,
            versionId: version.Id);

        var configEntries = await _configEntryRepository
            .GetAsync(searchEntryRequest, context.CancellationToken)
            .Select(Map)
            .ToArrayAsync(context.CancellationToken);

        _logger.LogGrpcGetConfigCompleted(serviceId, version.Id, configEntries.Length);

        return new GetConfigResponse
        {
            Entries = { configEntries },
            ServiceId = request.ServiceId,
            VersionId = version.Id.ToString(),
        };

        static GetConfigResponse.Types.ConfigEntry Map(ConfigEntry entry)
        {
            var timestamp = entry.UpdatedAt ?? entry.CreatedAt;

            return new GetConfigResponse.Types.ConfigEntry
            {
                Key = entry.Key,
                Value = entry.RawValue,
                Generation = entry.Generation,
                Timestamp = timestamp.UtcDateTime.ToTimestamp(),
                Type = entry.ValueType.ToProto(),
            };
        }
    }

    public override async Task<IsVersionExistResponse> IsVersionExists(IsVersionExistRequest request, ServerCallContext context)
    {
        var serviceId = new ServiceId(Guid.Parse(request.ServiceId));

        var searchVersionRequest = SearchVersionRequest.Create(
            serviceId: serviceId,
            label: request.Version);

        var version = await _configVersionsRepository
            .GetAsync(searchVersionRequest, context.CancellationToken)
            .SingleOrDefaultAsync(context.CancellationToken);

        _logger.LogGrpcVersionExistenceChecked(serviceId, request.Version, version?.Id);

        return new IsVersionExistResponse { VersionId = version?.Id.Value.ToString() };
    }

    public override async Task<CreateVersionResponse> CreateVersion(CreateVersionRequest request, ServerCallContext context)
    {
        var serviceId = new ServiceId(Guid.Parse(request.ServiceId));
        _logger.LogConfigVersionGenerateStarted(
            serviceId,
            request.Version,
            request.Classes.Sum(c => c.Entries.Count));

        var generateVersionRequest = new GenerateVersionRequest(
            ServiceId: serviceId,
            VersionLabel: request.Version,
            Description: $"Auto-generated by Konfigo at {_dateTimeProvider.GetNow():f}",
            Entries: request.Classes.SelectMany(Map).ToArray());

        var version = await _configVersionService.GenerateAsync(
            request: generateVersionRequest,
            cancellationToken: context.CancellationToken);

        _logger.LogConfigVersionGenerated(
            serviceId,
            version.Version.Id,
            version.Version.VersionLabel,
            version.Version.ConfigEntries.Count);

        return new CreateVersionResponse { VersionId = version.Version.Id.Value.ToString() };

        IEnumerable<GenerateVersionRequest.EntryRequest> Map(CreateVersionRequest.Types.ClassEntry entry)
            => entry.Entries.Select(c => MapEntry(c, entry));

        GenerateVersionRequest.EntryRequest MapEntry(
            CreateVersionRequest.Types.ClassEntry.Types.ConfigEntry c,
            CreateVersionRequest.Types.ClassEntry entry)
        {
            return new GenerateVersionRequest.EntryRequest(
                Key: c.Key,
                Name: c.Name,
                Description: c.Description,
                RawValue: c.Value,
                ValueType: c.ValueType.ToDomain(),
                EnumDefinition: c.EnumValues,
                GroupDescription: entry.Description,
                GroupName: entry.Name);
        }
    }

    public override async Task StartSubscribe(
        StartSubscribeRequest request,
        IServerStreamWriter<SubscriptionEvent> responseStream,
        ServerCallContext context)
    {
        var createSubscriberRequest = new CreateSubscriberRequest(
            ServiceId: new ServiceId(Guid.Parse(request.ServiceId)),
            VersionId: new VersionId(Guid.Parse(request.VersionId)));

        _logger.LogGrpcSubscriptionStarted(createSubscriberRequest.ServiceId, createSubscriberRequest.VersionId);

        await using var subscriber = await _updaterService.CreateAsync(
            request: createSubscriberRequest,
            cancellationToken: context.CancellationToken);

        var searchEntryRequest = SearchEntryRequest.Create(
            serviceId: createSubscriberRequest.ServiceId,
            versionId: createSubscriberRequest.VersionId,
            from: request.Timestamp.ToDateTimeOffset());

        var existsEntries = await _configEntryRepository
            .GetAsync(searchEntryRequest, context.CancellationToken)
            .Select(MapEntry)
            .ToArrayAsync(context.CancellationToken);

        if (existsEntries is not [])
        {
            _logger.LogGrpcSubscriptionBackfillSent(
                createSubscriberRequest.ServiceId,
                createSubscriberRequest.VersionId,
                existsEntries.Length);

            await responseStream.WriteAsync(new SubscriptionEvent { Events = { existsEntries } });
        }

        await foreach (var changeEvent in subscriber.SubscribeAsync(context.CancellationToken))
        {
            var configEvents = changeEvent.Requests.Select(MapRequest).ToArray();
            _logger.LogGrpcSubscriptionEventSent(
                createSubscriberRequest.ServiceId,
                createSubscriberRequest.VersionId,
                configEvents.Length);

            await responseStream.WriteAsync(new SubscriptionEvent { Events = { configEvents } });
        }

        return;

        static SubscriptionEvent.Types.ConfigEvent MapRequest(ChangeEvent.Request entry)
        {
            return new SubscriptionEvent.Types.ConfigEvent
            {
                Key = entry.Key,
                Value = entry.RawValue,
                Generation = entry.Generation,
                Timestamp = entry.Timestamp.ToTimestamp(),
                Type = entry.Type.ToProto(),
            };
        }

        static SubscriptionEvent.Types.ConfigEvent MapEntry(ConfigEntry entry)
        {
            var timestamp = entry.UpdatedAt ?? entry.CreatedAt;

            return new SubscriptionEvent.Types.ConfigEvent
            {
                Key = entry.Key,
                Value = entry.RawValue,
                Generation = entry.Generation,
                Timestamp = timestamp.UtcDateTime.ToTimestamp(),
                Type = entry.ValueType.ToProto(),
            };
        }
    }
}
