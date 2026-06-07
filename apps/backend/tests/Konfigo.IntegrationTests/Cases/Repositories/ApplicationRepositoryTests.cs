using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.IntegrationTests.DbHelpers;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.Repositories;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ApplicationRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;

    public ApplicationRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistService_WhenCalled()
    {
        // Arrange
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApplicationsRepository>();
        var service = BuildService();
        _serviceDbHelper.Track(service.Id.Value);

        // Act
        var added = await repo.AddAsync(service, CancellationToken.None);

        // Assert
        added.Id.Should().Be(service.Id);
        var row = await _serviceDbHelper.GetAsync(service.Id.Value);
        row.Should().NotBeNull();
        row!.Name.Should().Be(service.Name);
        row.Description.Should().Be(service.Description);
        row.RepositoryUrl.Should().Be(service.RepositoryUrl);
        row.ContactEmail.Should().Be(service.ContactEmail);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnService_WhenIdMatches()
    {
        // Arrange
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApplicationsRepository>();
        var service = BuildService();
        _serviceDbHelper.Track(service.Id.Value);
        await repo.AddAsync(service, CancellationToken.None);

        // Act
        var request = SearchServiceRequest.Create(ids: [service.Id]);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle(x => x.Id == service.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldFilterByName_WhenNameProvided()
    {
        // Arrange
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApplicationsRepository>();
        var marker = $"svc-{Guid.NewGuid():N}";
        var matching = BuildService(name: marker);
        var other = BuildService(name: $"other-{Guid.NewGuid():N}");
        _serviceDbHelper.Track(matching.Id.Value);
        _serviceDbHelper.Track(other.Id.Value);
        await repo.AddAsync(matching, CancellationToken.None);
        await repo.AddAsync(other, CancellationToken.None);

        // Act
        var request = SearchServiceRequest.Create(name: marker);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().ContainSingle().Which.Id.Should().Be(matching.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoMatch()
    {
        // Arrange
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApplicationsRepository>();

        // Act
        var request = SearchServiceRequest.Create(ids: [ServiceId.New()]);

        var results = await repo
            .GetAsync(request, CancellationToken.None)
            .ToArrayAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges_WhenCalled()
    {
        // Arrange
        var service = BuildService();
        _serviceDbHelper.Track(service.Id.Value);

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var addRepo = addScope.ServiceProvider.GetRequiredService<IApplicationsRepository>();
            await addRepo.AddAsync(service, CancellationToken.None);
        }

        // Act
        await using (var updateScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = updateScope.ServiceProvider.GetRequiredService<IApplicationsRepository>();

            var loaded = await repo
                .GetAsync(SearchServiceRequest.Create(ids: [service.Id]), CancellationToken.None)
                .SingleAsync();

            loaded.Name = "renamed";
            loaded.Description = "renamed desc";
            loaded.UpdatedAt = _now;

            await repo.UpdateAsync(loaded, CancellationToken.None);
        }

        // Assert
        var row = await _serviceDbHelper.GetAsync(service.Id.Value);
        row.Should().NotBeNull();
        row!.Name.Should().Be("renamed");
        row.Description.Should().Be("renamed desc");
        row.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveService_WhenCalled()
    {
        // Arrange
        var service = BuildService();
        _serviceDbHelper.Track(service.Id.Value);

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var addRepo = addScope.ServiceProvider.GetRequiredService<IApplicationsRepository>();
            await addRepo.AddAsync(service, CancellationToken.None);
        }

        // Act
        await using (var deleteScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = deleteScope.ServiceProvider.GetRequiredService<IApplicationsRepository>();

            var loaded = await repo
                .GetAsync(SearchServiceRequest.Create(ids: [service.Id]), CancellationToken.None)
                .SingleAsync();

            await repo.DeleteAsync(loaded, CancellationToken.None);
        }

        // Assert
        var row = await _serviceDbHelper.GetAsync(service.Id.Value);
        row.Should().BeNull();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _serviceDbHelper.DisposeAsync();

    private static ApplicationService BuildService(ServiceId? id = null, string? name = null)
    {
        return new ApplicationService
        {
            Id = id ?? ServiceId.New(),
            Name = name ?? $"svc-{Guid.NewGuid():N}",
            Description = "desc",
            RepositoryUrl = "https://repo",
            ContactEmail = "team@example.com",
            CreatedAt = _now,
        };
    }
}
