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
public sealed class AuditLogsRepositoryTests : IAsyncLifetime
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly IntegrationTestFixture _fixture;
    private readonly AuditLogDbHelper _auditLogDbHelper;

    public AuditLogsRepositoryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _auditLogDbHelper = new AuditLogDbHelper(connectionFactory);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistSingleLog_WhenCalled()
    {
        // Arrange
        var serviceId = ServiceId.New();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var log = BuildLog(serviceId);

        // Act
        await repo.AddAsync(log, CancellationToken.None);

        // Assert
        try
        {
            var count = await _auditLogDbHelper.CountByServiceAsync(serviceId.Value);
            count.Should().Be(1);
        }
        finally
        {
            await _auditLogDbHelper.DeleteByServiceAsync(serviceId.Value);
        }
    }

    [Fact]
    public async Task AddAsync_ShouldPersistMultipleLogs_WhenBatchProvided()
    {
        // Arrange
        var serviceId = ServiceId.New();
        await using var scope = _fixture.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var logs = new[] { BuildLog(serviceId), BuildLog(serviceId), BuildLog(serviceId) };

        // Act
        await repo.AddAsync(logs, CancellationToken.None);

        // Assert
        try
        {
            var count = await _auditLogDbHelper.CountByServiceAsync(serviceId.Value);
            count.Should().Be(3);
        }
        finally
        {
            await _auditLogDbHelper.DeleteByServiceAsync(serviceId.Value);
        }
    }

    [Fact]
    public async Task GetAsync_ShouldReturnLogsForService_WhenServiceIdMatches()
    {
        // Arrange
        var serviceId = ServiceId.New();
        var otherServiceId = ServiceId.New();

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = addScope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            await repo.AddAsync([BuildLog(serviceId), BuildLog(serviceId), BuildLog(otherServiceId)], CancellationToken.None);
        }

        try
        {
            // Act
            await using var scope = _fixture.Services.CreateAsyncScope();
            var queryRepo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            var request = SearchAuditLogRequest.Create(serviceId: serviceId);

            var results = await queryRepo
                .GetAsync(request, CancellationToken.None)
                .ToArrayAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().OnlyContain(x => x.ServiceId == serviceId);
        }
        finally
        {
            await _auditLogDbHelper.DeleteByServiceAsync(serviceId.Value);
            await _auditLogDbHelper.DeleteByServiceAsync(otherServiceId.Value);
        }
    }

    [Fact]
    public async Task GetAsync_ShouldRespectPageSize_WhenLimited()
    {
        // Arrange
        var serviceId = ServiceId.New();

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = addScope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            await repo.AddAsync(
                [BuildLog(serviceId), BuildLog(serviceId), BuildLog(serviceId)],
                CancellationToken.None);
        }

        try
        {
            // Act
            await using var scope = _fixture.Services.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            var request = SearchAuditLogRequest.Create(serviceId: serviceId, pageSize: 2);

            var results = await repo
                .GetAsync(request, CancellationToken.None)
                .ToArrayAsync();

            // Assert
            results.Should().HaveCount(2);
            results.Should().BeInDescendingOrder(x => x.Num);
        }
        finally
        {
            await _auditLogDbHelper.DeleteByServiceAsync(serviceId.Value);
        }
    }

    [Fact]
    public async Task GetAsync_ShouldHonorCursor_WhenCursorProvided()
    {
        // Arrange
        var serviceId = ServiceId.New();

        await using (var addScope = _fixture.Services.CreateAsyncScope())
        {
            var repo = addScope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            await repo.AddAsync(
                [BuildLog(serviceId), BuildLog(serviceId), BuildLog(serviceId)],
                CancellationToken.None);
        }

        try
        {
            // Act
            await using var scope = _fixture.Services.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

            var firstPage = await repo
                .GetAsync(SearchAuditLogRequest.Create(serviceId: serviceId, pageSize: 2), CancellationToken.None)
                .ToArrayAsync();

            var cursor = new SearchAuditLogRequest.PageToken(firstPage[^1].Num);

            var secondPage = await repo
                .GetAsync(SearchAuditLogRequest.Create(serviceId: serviceId, pageSize: 2, cursor: cursor), CancellationToken.None)
                .ToArrayAsync();

            // Assert
            firstPage.Should().HaveCount(2);
            secondPage.Should().HaveCount(1);
            var firstPageIds = firstPage.Select(x => x.Id).ToHashSet();
            secondPage.Should().OnlyContain(x => !firstPageIds.Contains(x.Id));
        }
        finally
        {
            await _auditLogDbHelper.DeleteByServiceAsync(serviceId.Value);
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _auditLogDbHelper.DisposeAsync();

    private static AuditLog BuildLog(ServiceId serviceId)
    {
        return new AuditLog
        {
            Id = LogId.New(),
            ServiceId = serviceId,
            UserId = new UserId(Guid.NewGuid().ToString()),
            Entry = new ServiceCreatedEntry(
                Name: $"svc-{Guid.NewGuid():N}",
                Description: "desc",
                RepositoryUrl: null,
                GitLabProjectId: null,
                ContactEmail: null),
            CreatedAt = _now,
        };
    }
}
