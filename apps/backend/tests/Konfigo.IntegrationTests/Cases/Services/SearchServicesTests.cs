using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Controllers.Models.Services;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.IntegrationTests.DbHelpers;
using Konfigo.IntegrationTests.Shared.Extensions;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.Services;

[Collection(nameof(IntegrationTestCollection))]
public sealed class SearchServicesTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;

    public SearchServicesTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Search_ShouldReturnCreatedService_WhenWildcardAllowed()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var marker = $"svc-search-{Guid.NewGuid():N}";
        var created = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = marker });
        _serviceDbHelper.Track(created.Id);

        var request = new SearchServicesRequest { Name = marker, PageSize = 10 };

        // Act
        var page = await client.SearchServicesAsync(request);

        // Assert
        page.Entities.Should().NotBeNull();
        page.Entities.Should().Contain(e => e.Id == created.Id);
    }

    [Fact]
    public async Task Search_ShouldReturnServices_WhenCanChangeHasNoServiceClaims()
    {
        // Arrange
        using var admin = _fixture.CreateAdminClient();
        var marker = $"svc-search-{Guid.NewGuid():N}";
        var created = await admin.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = marker });
        _serviceDbHelper.Track(created.Id);

        using var client = _fixture.CreateAuthenticatedClient(roles: "developer", services: string.Empty);
        var request = new SearchServicesRequest { Name = marker, PageSize = 10 };

        // Act
        var page = await client.SearchServicesAsync(request);

        // Assert
        page.Entities.Should().Contain(e => e.Id == created.Id);
    }

    [Fact]
    public async Task Search_ShouldReturnBadRequest_WhenPageSizeIsZero()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var request = new SearchServicesRequest { PageSize = 0 };

        // Act
        var response = await client.SendSearchServicesAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _serviceDbHelper.DisposeAsync();
}
