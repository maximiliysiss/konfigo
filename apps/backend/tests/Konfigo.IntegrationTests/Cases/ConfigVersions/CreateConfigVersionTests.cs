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
public sealed class CreateConfigVersionTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;

    public CreateConfigVersionTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Create_ShouldCreate_WhenAdmin()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);
        var request = new CreateConfigVersionRequest
        {
            VersionLabel = $"v-{Guid.NewGuid():N}",
            Description = "Test version",
        };

        // Act
        var created = await client.CreateConfigVersionAsync(service.Id, request);

        // Assert
        _versionDbHelper.Track(created.Id);

        var row = await _versionDbHelper.GetAsync(created.Id);
        row.Should().NotBeNull();
        row!.ServiceId.Should().Be(service.Id);
        row.VersionLabel.Should().Be(request.VersionLabel);
        row.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var adminClient = _fixture.CreateAdminClient();
        var service = await adminClient.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        using var client = _fixture.CreateUserClient();
        var request = new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" };

        // Act
        var response = await client.SendCreateConfigVersionAsync(service.Id, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = _fixture.CreateAnonymousClient();
        var request = new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" };

        // Act
        var response = await client.SendCreateConfigVersionAsync(Guid.NewGuid(), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }
}
