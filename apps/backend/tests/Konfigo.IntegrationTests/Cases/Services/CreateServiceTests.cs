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
public sealed class CreateServiceTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;

    public CreateServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Create_ShouldCreate_WhenAdmin()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var request = new CreateOrUpdateServiceRequest
        {
            Name = $"svc-{Guid.NewGuid():N}",
            Description = "Test service",
            RepositoryUrl = "https://gitlab.com/test/repo",
            ContactEmail = "owner@pnlfin.tech",
        };

        // Act
        var created = await client.CreateServiceAsync(request);

        // Assert
        _serviceDbHelper.Track(created.Id);

        var row = await _serviceDbHelper.GetAsync(created.Id);
        row.Should().NotBeNull();
        row!.Name.Should().Be(request.Name);
        row.Description.Should().Be(request.Description);
        row.RepositoryUrl.Should().Be(request.RepositoryUrl);
        row.ContactEmail.Should().Be(request.ContactEmail);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();
        var request = new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" };

        // Act
        var response = await client.SendCreateServiceAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = _fixture.CreateAnonymousClient();
        var request = new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" };

        // Act
        var response = await client.SendCreateServiceAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _serviceDbHelper.DisposeAsync();
}
