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
public sealed class ConfigEntryRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public ConfigEntryRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntry_WhenCalled()
    {
        // Arrange
        var (_, versionId) = await SeedServiceAndVersionAsync();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();
        var entry = BuildEntry(versionId);
        _entryDbHelper.Track(entry.Id.Value);

        // Act
        var added = await repo.AddAsync(entry, CancellationToken.None);

        // Assert
        added.Id.Should().Be(entry.Id);
        var row = await _entryDbHelper.GetAsync(entry.Id.Value);
        row.Should().NotBeNull();
        row!.ConfigVersionId.Should().Be(versionId.Value);
        row.Key.Should().Be(entry.Key);
        row.Name.Should().Be(entry.Name);
        row.RawValue.Should().Be(entry.RawValue);
        row.ValueType.Should().Be((int)entry.ValueType);
        row.Generation.Should().Be(entry.Generation);
    }

    [Fact]
    public async Task GetAsync_ShouldFilterByVersionId_WhenCalled()
    {
        // Arrange
        var (serviceId, versionA) = await SeedServiceAndVersionAsync();
        var (_, versionB) = await SeedServiceAndVersionAsync();

        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();

        var entryA = BuildEntry(versionA);
        var entryB = BuildEntry(versionB);
        _entryDbHelper.Track(entryA.Id.Value);
        _entryDbHelper.Track(entryB.Id.Value);
        await repo.AddAsync(entryA, CancellationToken.None);
        await repo.AddAsync(entryB, CancellationToken.None);

        // Act
        var request = SearchEntryRequest.Create(serviceId: serviceId, versionId: versionA);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle().Which.Id.Should().Be(entryA.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldFilterByIds_WhenIdsProvided()
    {
        // Arrange
        var (serviceId, versionId) = await SeedServiceAndVersionAsync();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();

        var first = BuildEntry(versionId);
        var second = BuildEntry(versionId);
        _entryDbHelper.Track(first.Id.Value);
        _entryDbHelper.Track(second.Id.Value);
        await repo.AddAsync(first, CancellationToken.None);
        await repo.AddAsync(second, CancellationToken.None);

        // Act
        var request = SearchEntryRequest.Create(
            serviceId: serviceId,
            versionId: versionId,
            ids: [second.Id]);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle().Which.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldFilterByFrom_WhenFromProvided()
    {
        // Arrange
        var (serviceId, versionId) = await SeedServiceAndVersionAsync();

        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();

        var oldEntry = BuildEntry(versionId, createdAt: _now.AddDays(-2));
        var newEntry = BuildEntry(versionId, createdAt: _now);
        _entryDbHelper.Track(oldEntry.Id.Value);
        _entryDbHelper.Track(newEntry.Id.Value);
        await repo.AddAsync(oldEntry, CancellationToken.None);
        await repo.AddAsync(newEntry, CancellationToken.None);

        // Act
        var request = SearchEntryRequest.Create(
            serviceId: serviceId,
            versionId: versionId,
            from: _now.AddDays(-1));

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle().Which.Id.Should().Be(newEntry.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges_WhenCalled()
    {
        // Arrange
        var (serviceId, versionId) = await SeedServiceAndVersionAsync();
        var entry = BuildEntry(versionId);
        _entryDbHelper.Track(entry.Id.Value);

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var addRepo = addScope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();
            await addRepo.AddAsync(entry, CancellationToken.None);
        }

        // Act
        await using (var updateScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = updateScope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();

            var searchEntryRequest = SearchEntryRequest.Create(serviceId: serviceId, versionId: versionId, ids: [entry.Id]);

            var loaded = await repo
                .GetAsync(searchEntryRequest, CancellationToken.None)
                .SingleAsync();

            loaded.RawValue = "updated";
            loaded.Generation += 1;
            loaded.UpdatedAt = _now;

            await repo.UpdateAsync([loaded], CancellationToken.None);
        }

        // Assert
        var row = await _entryDbHelper.GetAsync(entry.Id.Value);
        row.Should().NotBeNull();
        row!.RawValue.Should().Be("updated");
        row.Generation.Should().Be(entry.Generation + 1);
        row.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntry_WhenCalled()
    {
        // Arrange
        var (serviceId, versionId) = await SeedServiceAndVersionAsync();
        var entry = BuildEntry(versionId);
        _entryDbHelper.Track(entry.Id.Value);

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var addRepo = addScope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();
            await addRepo.AddAsync(entry, CancellationToken.None);
        }

        // Act
        await using (var deleteScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = deleteScope.ServiceProvider.GetRequiredService<IConfigEntryRepository>();

            var searchEntryRequest = SearchEntryRequest.Create(
                serviceId: serviceId,
                versionId: versionId,
                ids: [entry.Id]);

            var loaded = await repo
                .GetAsync(searchEntryRequest, CancellationToken.None)
                .SingleAsync();

            await repo.DeleteAsync(loaded, CancellationToken.None);
        }

        // Assert
        var row = await _entryDbHelper.GetAsync(entry.Id.Value);
        row.Should().BeNull();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _entryDbHelper.DisposeAsync();
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }

    private async Task<(ServiceId ServiceId, VersionId VersionId)> SeedServiceAndVersionAsync()
    {
        await using var scope = _fixture.Services.CreateAsyncScope();
        var serviceRepo = scope.ServiceProvider.GetRequiredService<IApplicationsRepository>();
        var versionRepo = scope.ServiceProvider.GetRequiredService<IConfigVersionsRepository>();

        var service = new ApplicationService
        {
            Id = ServiceId.New(),
            Name = $"svc-{Guid.NewGuid():N}",
            Description = null,
            RepositoryUrl = null,
            GitLabProjectId = null,
            ContactEmail = null,
            CreatedAt = _now,
        };
        _serviceDbHelper.Track(service.Id.Value);
        await serviceRepo.AddAsync(service, CancellationToken.None);

        var version = new ConfigVersion
        {
            Id = VersionId.New(),
            ServiceId = service.Id,
            VersionLabel = "v1",
            Description = null,
            CreatedAt = _now,
        };
        _versionDbHelper.Track(version.Id.Value);
        await versionRepo.AddAsync(version, CancellationToken.None);

        return (service.Id, version.Id);
    }

    private static ConfigEntry BuildEntry(VersionId versionId, DateTimeOffset? createdAt = null)
    {
        return new ConfigEntry
        {
            Id = EntryId.New(),
            ConfigVersionId = versionId,
            Key = $"k-{Guid.NewGuid():N}",
            Name = "Key",
            RawValue = "value",
            ValueType = ConfigValueType.String,
            Description = "desc",
            GroupName = "g",
            GroupDescription = "gd",
            Generation = 1,
            CreatedAt = createdAt ?? _now,
        };
    }
}
