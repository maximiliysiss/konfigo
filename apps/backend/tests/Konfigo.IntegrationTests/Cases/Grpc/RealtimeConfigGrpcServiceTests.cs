using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Konfigo.Application.Services.Updater;
using Konfigo.Application.Services.Updater.Models;
using Konfigo.Client.Grpc;
using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.IntegrationTests.DbHelpers;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using GrpcValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.IntegrationTests.Cases.Grpc;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RealtimeConfigGrpcServiceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public RealtimeConfigGrpcServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;

        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task GetConfig_ShouldReturnEntries_WhenVersionExists()
    {
        // Arrange
        var serviceId = await InsertServiceAsync();
        var versionId = await InsertVersionAsync(serviceId, "v-get-config");
        var createdAt = StartedAt.AddMinutes(1);
        await InsertEntryAsync(
            versionId,
            key: "grpc.get-config",
            rawValue: "enabled",
            generation: 3,
            createdAt: createdAt);

        using var channel = CreateGrpcChannel();
        var client = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);

        // Act
        var response = await client.GetConfigAsync(new GetConfigRequest
        {
            ServiceId = serviceId.ToString(),
            Version = "v-get-config",
        });

        // Assert
        response.Entries.Should().ContainSingle();
        var entry = response.Entries.Single();
        entry.Key.Should().Be("grpc.get-config");
        entry.Value.Should().Be("enabled");
        entry.Generation.Should().Be(3);
        entry.Timestamp.ToDateTimeOffset().Should().BeCloseTo(createdAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetConfig_ShouldReturnEmptyResponse_WhenVersionDoesNotExist()
    {
        // Arrange
        var serviceId = await InsertServiceAsync();

        using var channel = CreateGrpcChannel();
        var client = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);

        // Act
        var response = await client.GetConfigAsync(new GetConfigRequest
        {
            ServiceId = serviceId.ToString(),
            Version = "missing",
        });

        // Assert
        response.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task IsVersionExists_ShouldReturnVersionId_WhenVersionExists()
    {
        // Arrange
        var serviceId = await InsertServiceAsync();
        var versionId = await InsertVersionAsync(serviceId, "v-exists");

        using var channel = CreateGrpcChannel();
        var client = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);

        // Act
        var response = await client.IsVersionExistsAsync(new IsVersionExistRequest
        {
            ServiceId = serviceId.ToString(),
            Version = "v-exists",
        });

        // Assert
        response.VersionId.Should().Be(versionId.ToString());
    }

    [Fact]
    public async Task IsVersionExists_ShouldReturnEmptyVersionId_WhenVersionDoesNotExist()
    {
        // Arrange
        var serviceId = await InsertServiceAsync();

        using var channel = CreateGrpcChannel();
        var client = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);

        // Act
        var response = await client.IsVersionExistsAsync(new IsVersionExistRequest
        {
            ServiceId = serviceId.ToString(),
            Version = "missing",
        });

        // Assert
        response.VersionId.Should().BeNull();
    }

    [Fact]
    public async Task CreateVersion_ShouldCreateVersionAndEntries()
    {
        // Arrange
        var serviceId = await InsertServiceAsync();
        var versionLabel = $"v-grpc-create-{Guid.NewGuid():N}";

        using var channel = CreateGrpcChannel();
        var client = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);

        // Act
        var response = await client.CreateVersionAsync(new CreateVersionRequest
        {
            ServiceId = serviceId.ToString(),
            Version = versionLabel,
            Classes =
            {
                new CreateVersionRequest.Types.ClassEntry
                {
                    Name = "Feature flags",
                    Description = "Runtime switches",
                    Entries =
                    {
                        new CreateVersionRequest.Types.ClassEntry.Types.ConfigEntry
                        {
                            Key = "feature.grpc",
                            Name = "Feature Grpc",
                            Description = "Enables gRPC path",
                            ValueType = GrpcValueType.Boolean,
                            Value = "true",
                        },
                        new CreateVersionRequest.Types.ClassEntry.Types.ConfigEntry
                        {
                            Key = "feature.limit",
                            Name = "Feature Limit",
                            ValueType = GrpcValueType.Number,
                            Value = "10",
                        },
                    },
                },
            },
        });

        // Assert
        var versionId = Guid.Parse(response.VersionId);
        _versionDbHelper.Track(versionId);

        var version = await _versionDbHelper.GetAsync(versionId);
        version.Should().NotBeNull();
        version!.ServiceId.Should().Be(serviceId);
        version.VersionLabel.Should().Be(versionLabel);

        var config = await client.GetConfigAsync(new GetConfigRequest
        {
            ServiceId = serviceId.ToString(),
            Version = versionLabel,
        });

        config.Entries.Should().HaveCount(2);
        config.Entries.Should().Contain(e => e.Key == "feature.grpc" && e.Value == "true" && e.Generation == 1);
        config.Entries.Should().Contain(e => e.Key == "feature.limit" && e.Value == "10" && e.Generation == 1);
    }

    [Fact]
    public async Task StartSubscribe_ShouldSendExistingEntriesChangedAfterTimestamp()
    {
        // Arrange
        var serviceId = await InsertServiceAsync();
        var versionId = await InsertVersionAsync(serviceId, "v-subscribe-backfill");
        var from = StartedAt.AddMinutes(1);
        await InsertEntryAsync(
            versionId,
            key: "grpc.backfill",
            rawValue: "backfilled",
            generation: 2,
            createdAt: from.AddSeconds(1));

        using var channel = CreateGrpcChannel();
        var client = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act
        using var call = client.StartSubscribe(new StartSubscribeRequest
        {
            ServiceId = serviceId.ToString(),
            VersionId = versionId.ToString(),
            Timestamp = from.UtcDateTime.ToTimestamp(),
        }, cancellationToken: cts.Token);

        var hasEvent = await call.ResponseStream.MoveNext(cts.Token);

        // Assert
        hasEvent.Should().BeTrue();
        var entry = call.ResponseStream.Current.Events.Should().ContainSingle().Subject;
        entry.Key.Should().Be("grpc.backfill");
        entry.Value.Should().Be("backfilled");
        entry.Generation.Should().Be(2);
    }

    [Fact]
    public async Task StartSubscribe_ShouldStreamPublishedChanges()
    {
        // Arrange
        var serviceId = await InsertServiceAsync();
        var versionId = await InsertVersionAsync(serviceId, "v-subscribe-live");

        using var channel = CreateGrpcChannel();
        var client = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var call = client.StartSubscribe(new StartSubscribeRequest
        {
            ServiceId = serviceId.ToString(),
            VersionId = versionId.ToString(),
            Timestamp = StartedAt.AddDays(1).UtcDateTime.ToTimestamp(),
        }, cancellationToken: cts.Token);

        var readTask = call.ResponseStream.MoveNext(cts.Token);
        var updaterService = _fixture.Services.GetRequiredService<IUpdaterService>();
        var changeEvent = new ChangeEvent(
            ServiceId: new ServiceId(serviceId),
            VersionId: new VersionId(versionId),
            Requests:
            [
                new ChangeEvent.Request(
                    EntryId: EntryId.New(),
                    Key: "grpc.live",
                    Type: ConfigValueType.String,
                    RawValue: "streamed",
                    Generation: 7,
                    Timestamp: StartedAt.AddMinutes(2).UtcDateTime)
            ]);

        // Act
        while (!readTask.IsCompleted)
        {
            await updaterService.PublishAsync(changeEvent, cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
        }

        var hasEvent = await readTask;

        // Assert
        hasEvent.Should().BeTrue();
        var entry = call.ResponseStream.Current.Events.Should().ContainSingle().Subject;
        entry.Key.Should().Be("grpc.live");
        entry.Value.Should().Be("streamed");
        entry.Generation.Should().Be(7);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _entryDbHelper.DisposeAsync();
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }

    private GrpcChannel CreateGrpcChannel() =>
        GrpcChannel.ForAddress(
            _fixture.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = _fixture.Server.CreateHandler() });

    private async Task<Guid> InsertServiceAsync()
    {
        var serviceId = Guid.NewGuid();
        await _serviceDbHelper.InsertAsync(new ApplicationServiceDbHelper.TableRow
        {
            Id = serviceId,
            Name = $"svc-grpc-{Guid.NewGuid():N}",
            CreatedAt = StartedAt,
        });

        return serviceId;
    }

    private async Task<Guid> InsertVersionAsync(Guid serviceId, string versionLabel)
    {
        var versionId = Guid.NewGuid();
        await _versionDbHelper.InsertAsync(new ConfigVersionDbHelper.TableRow
        {
            Id = versionId,
            ServiceId = serviceId,
            VersionLabel = versionLabel,
            Description = "gRPC integration test version",
            CreatedAt = StartedAt,
        });

        return versionId;
    }

    private async Task<Guid> InsertEntryAsync(
        Guid versionId,
        string key,
        string? rawValue,
        int generation,
        DateTimeOffset createdAt)
    {
        var entryId = Guid.NewGuid();
        await _entryDbHelper.InsertAsync(new ConfigEntryDbHelper.TableRow
        {
            Id = entryId,
            ConfigVersionId = versionId,
            Key = key,
            Name = key,
            RawValue = rawValue,
            ValueType = (int)ConfigValueType.String,
            Generation = generation,
            CreatedAt = createdAt,
        });

        return entryId;
    }

}
