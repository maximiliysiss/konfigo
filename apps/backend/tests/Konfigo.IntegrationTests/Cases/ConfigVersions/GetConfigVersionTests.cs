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
public sealed class GetConfigVersionTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;

    public GetConfigVersionTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
    }

    [Fact]
    public async Task GetAll_ShouldReturnVersions()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var created = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(created.Id);
        var created1 = await client.CreateConfigVersionAsync(
            created.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(created1.Id);
        var version = created1;

        // Act
        var versions = await client.GetConfigVersionsAsync(created.Id);

        // Assert
        versions.Should().Contain(v => v.Id == version.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnVersion_WhenExists()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var createConfigVersionRequest = new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" };
        var version = await client.CreateConfigVersionAsync(service.Id, createConfigVersionRequest);
        _versionDbHelper.Track(version.Id);

        // Act
        var fetched = await client.GetConfigVersionAsync(service.Id, version.Id);

        // Assert
        fetched.Id.Should().Be(version.Id);
        fetched.VersionLabel.Should().Be(version.VersionLabel);
    }

    [Fact]
    public async Task GetById_ShouldReturnEmptyBody_WhenVersionDoesNotExist()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        // Act
        var response = await client.GetAsync($"/api/configversions/{service.Id}/{Guid.NewGuid()}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        (string.IsNullOrEmpty(content) || content == "null").Should().BeTrue();
    }

    [Fact]
    public async Task GetById_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();

        // Act
        var response = await client.GetAsync($"/api/configversions/{Guid.NewGuid()}/{Guid.NewGuid()}");

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
