using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;
using Konfigo.IntegrationTests.DbHelpers;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.Repositories;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ConfigVersionsRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public ConfigVersionsRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistVersion_WhenCalled()
    {
        // Arrange
        var serviceId = await SeedServiceAsync();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();
        var version = BuildVersion(serviceId: serviceId);
        _versionDbHelper.Track(version.Id.Value);

        // Act
        var added = await repo.AddAsync(version, CancellationToken.None);

        // Assert
        added.Id.Should().Be(version.Id);
        var row = await _versionDbHelper.GetAsync(version.Id.Value);
        row.Should().NotBeNull();
        row!.ServiceId.Should().Be(serviceId.Value);
        row.VersionLabel.Should().Be(version.VersionLabel);
        row.Description.Should().Be(version.Description);
    }

    [Fact]
    public async Task GetAsync_ShouldFilterByServiceId_WhenCalled()
    {
        // Arrange
        var serviceA = await SeedServiceAsync();
        var serviceB = await SeedServiceAsync();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();

        var versionA = BuildVersion(serviceId: serviceA);
        var versionB = BuildVersion(serviceId: serviceB);
        _versionDbHelper.Track(versionA.Id.Value);
        _versionDbHelper.Track(versionB.Id.Value);
        await repo.AddAsync(versionA, CancellationToken.None);
        await repo.AddAsync(versionB, CancellationToken.None);

        // Act
        var request = SearchVersionRequest.Create(serviceId: serviceA);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle().Which.Id.Should().Be(versionA.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldFilterByIds_WhenIdsProvided()
    {
        // Arrange
        var serviceId = await SeedServiceAsync();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();

        var first = BuildVersion(serviceId: serviceId, versionLabel: "v1");
        var second = BuildVersion(serviceId: serviceId, versionLabel: "v2");
        _versionDbHelper.Track(first.Id.Value);
        _versionDbHelper.Track(second.Id.Value);
        await repo.AddAsync(first, CancellationToken.None);
        await repo.AddAsync(second, CancellationToken.None);

        // Act
        var request = SearchVersionRequest.Create(serviceId: serviceId, ids: [second.Id]);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle().Which.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldFilterByLabel_WhenLabelProvided()
    {
        // Arrange
        var serviceId = await SeedServiceAsync();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();

        var label = $"label-{Guid.NewGuid():N}";
        var matching = BuildVersion(serviceId: serviceId, versionLabel: label);
        var other = BuildVersion(serviceId: serviceId, versionLabel: "other");
        _versionDbHelper.Track(matching.Id.Value);
        _versionDbHelper.Track(other.Id.Value);
        await repo.AddAsync(matching, CancellationToken.None);
        await repo.AddAsync(other, CancellationToken.None);

        // Act
        var request = SearchVersionRequest.Create(serviceId: serviceId, label: label);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle().Which.Id.Should().Be(matching.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldIncludeEntries_WhenIncludeEntryRequested()
    {
        // Arrange
        var serviceId = await SeedServiceAsync();
        var version = BuildVersion(serviceId: serviceId);
        _versionDbHelper.Track(version.Id.Value);

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var versionRepo = addScope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();
            await versionRepo.AddAsync(version, CancellationToken.None);

            var entryRepo = addScope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();
            var entry = BuildEntry(versionId: version.Id);
            _entryDbHelper.Track(entry.Id.Value);
            await entryRepo.AddAsync(entry, CancellationToken.None);
        }

        // Act
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();

        var request = SearchVersionRequest.Create(serviceId: serviceId, ids: [version.Id], include: [EEntityType.Entry]);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        var loaded = results.Should().ContainSingle().Subject;
        loaded.ConfigEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges_WhenCalled()
    {
        // Arrange
        var serviceId = await SeedServiceAsync();
        var version = BuildVersion(serviceId: serviceId);
        _versionDbHelper.Track(version.Id.Value);

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var addRepo = addScope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();
            await addRepo.AddAsync(version, CancellationToken.None);
        }

        // Act
        await using (var updateScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = updateScope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();

            var loaded = await repo
                .GetAsync(SearchVersionRequest.Create(serviceId: serviceId, ids: [version.Id]), CancellationToken.None)
                .SingleAsync();

            loaded.VersionLabel = "v-renamed";
            loaded.Description = "v desc renamed";
            loaded.UpdatedAt = _now;
            await repo.UpdateAsync(loaded, CancellationToken.None);
        }

        // Assert
        var row = await _versionDbHelper.GetAsync(version.Id.Value);
        row.Should().NotBeNull();
        row!.VersionLabel.Should().Be("v-renamed");
        row.Description.Should().Be("v desc renamed");
        row.UpdatedAt.Should().NotBeNull();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _entryDbHelper.DisposeAsync();
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }

    private async Task<ServiceId> SeedServiceAsync()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApplicationsRepository>();
        var service = new ApplicationService
        {
            Id = ServiceId.New(),
            Name = $"svc-{Guid.NewGuid():N}",
            Description = "desc",
            RepositoryUrl = null,
            GitLabProjectId = null,
            ContactEmail = null,
            CreatedAt = _now,
        };
        _serviceDbHelper.Track(service.Id.Value);
        await repo.AddAsync(service, CancellationToken.None);
        return service.Id;
    }

    private static ConfigVersion BuildVersion(ServiceId serviceId, VersionId? id = null, string versionLabel = "v1")
    {
        return new ConfigVersion
        {
            Id = id ?? VersionId.New(),
            ServiceId = serviceId,
            VersionLabel = versionLabel,
            Description = "ver desc",
            CreatedAt = _now,
        };
    }

    private static ConfigEntry BuildEntry(VersionId versionId)
    {
        return new ConfigEntry
        {
            Id = EntryId.New(),
            ConfigVersionId = versionId,
            Key = $"k-{Guid.NewGuid():N}",
            Name = "Name",
            RawValue = "v",
            ValueType = ConfigValueType.String,
            Description = "desc",
            GroupName = "g",
            GroupDescription = "gd",
            Generation = 1,
            CreatedAt = _now,
        };
    }
}
