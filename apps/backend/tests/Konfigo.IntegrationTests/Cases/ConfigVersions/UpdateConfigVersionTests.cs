using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Controllers.Models.Services;
using Konfigo.Controllers.Models.Versions;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.IntegrationTests.DbHelpers;
using Konfigo.IntegrationTests.Shared.Extensions;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.ConfigVersions;

[Collection(nameof(IntegrationTestCollection))]
public sealed class UpdateConfigVersionTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;

    public UpdateConfigVersionTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Update_ShouldUpdateLabelAndDescription_WhenAdmin()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var createConfigVersionRequest = new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}", Description = "Initial" };
        var version = await client.CreateConfigVersionAsync(service.Id, createConfigVersionRequest);
        _versionDbHelper.Track(version.Id);

        var updateRequest = new UpdateConfigVersionRequest
        {
            VersionLabel = $"v-renamed-{Guid.NewGuid():N}",
            Description = "Updated description",
        };

        // Act
        var response = await client.SendUpdateConfigVersionAsync(service.Id, version.Id, updateRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(content);

        var row = await _versionDbHelper.GetAsync(version.Id);
        row.Should().NotBeNull();
        row!.VersionLabel.Should().Be(updateRequest.VersionLabel);
        row.Description.Should().Be(updateRequest.Description);
        row.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ShouldReturnEmptyBody_WhenVersionDoesNotExist()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var updateRequest = new UpdateConfigVersionRequest
        {
            VersionLabel = $"v-renamed-{Guid.NewGuid():N}",
            Description = "Updated description",
        };

        // Act
        var response = await client.SendUpdateConfigVersionAsync(service.Id, Guid.NewGuid(), updateRequest);

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
        var updateRequest = new UpdateConfigVersionRequest
        {
            VersionLabel = $"v-renamed-{Guid.NewGuid():N}",
            Description = "Updated description",
        };

        // Act
        var response = await client.SendUpdateConfigVersionAsync(Guid.NewGuid(), Guid.NewGuid(), updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }
}
