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
public sealed class UpdateServiceTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;

    public UpdateServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Update_ShouldUpdate_WhenAdmin()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var created = await client.CreateServiceAsync(
            new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}", Description = "original" });
        _serviceDbHelper.Track(created.Id);

        var updateRequest = new CreateOrUpdateServiceRequest
        {
            Name = $"svc-renamed-{Guid.NewGuid():N}",
            Description = "updated",
            RepositoryUrl = "https://gitlab.com/test/renamed",
            ContactEmail = "owner2@pnlfin.tech",
        };

        // Act
        var response = await client.SendUpdateServiceAsync(created.Id, updateRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(content);

        var row = await _serviceDbHelper.GetAsync(created.Id);
        row.Should().NotBeNull();
        row!.Name.Should().Be(updateRequest.Name);
        row.Description.Should().Be(updateRequest.Description);
        row.RepositoryUrl.Should().Be(updateRequest.RepositoryUrl);
        row.ContactEmail.Should().Be(updateRequest.ContactEmail);
        row.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ShouldReturnEmptyBody_WhenServiceDoesNotExist()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var updateRequest = new CreateOrUpdateServiceRequest
        {
            Name = $"svc-renamed-{Guid.NewGuid():N}",
            Description = "updated",
        };

        // Act
        var response = await client.SendUpdateServiceAsync(Guid.NewGuid(), updateRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        (string.IsNullOrEmpty(content) || content == "null").Should().BeTrue();
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();
        var updateRequest = new CreateOrUpdateServiceRequest { Name = $"svc-renamed-{Guid.NewGuid():N}" };

        // Act
        var response = await client.SendUpdateServiceAsync(Guid.NewGuid(), updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _serviceDbHelper.DisposeAsync();
}
