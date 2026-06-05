using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Controllers.Models.Audit;
using Konfigo.Controllers.Models.Services;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.IntegrationTests.DbHelpers;
using Konfigo.IntegrationTests.Shared.Extensions;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.AuditLogs;

[Collection(nameof(IntegrationTestCollection))]
public sealed class SearchAuditLogsTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly AuditLogDbHelper _auditLogDbHelper;

    public SearchAuditLogsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _auditLogDbHelper = new AuditLogDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Search_ShouldReturnAuditEntries_AfterServiceMutation()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        // Act
        var page = await client.SearchAuditLogsAsync(service.Id, new SearchAuditRequest { PageSize = 50 });

        // Assert
        page.Entities.Should().NotBeNull();
        page.Entities.Length.Should().BeGreaterThan(0);

        // Cleanup audit logs left for this service so the helper test stays clean
        await _auditLogDbHelper.DeleteByServiceAsync(service.Id);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenPageSizeIsZero()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        // Act
        var response = await client.PostAsKonfigoJsonAsync(
            $"/api/audit/{Guid.NewGuid()}/search",
            new SearchAuditRequest { PageSize = 0 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _auditLogDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }
}
