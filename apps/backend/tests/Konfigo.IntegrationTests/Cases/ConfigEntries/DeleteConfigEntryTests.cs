using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Controllers.Models.Entry;
using Konfigo.Controllers.Models.Services;
using Konfigo.Controllers.Models.Versions;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Domain.Enums;
using Konfigo.IntegrationTests.DbHelpers;
using Konfigo.IntegrationTests.Shared.Extensions;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.ConfigEntries;

[Collection(nameof(IntegrationTestCollection))]
public sealed class DeleteConfigEntryTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public DeleteConfigEntryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Delete_ShouldRemoveFromDb_WhenAdmin()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var createOrUpdateServiceRequest = new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" };
        var service = await client.CreateServiceAsync(createOrUpdateServiceRequest);
        _serviceDbHelper.Track(service.Id);

        var createConfigVersionRequest = new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" };
        var version = await client.CreateConfigVersionAsync(service.Id, createConfigVersionRequest);
        _versionDbHelper.Track(version.Id);

        var createConfigEntryRequest = new CreateConfigEntryRequest
        {
            Key = $"key-{Guid.NewGuid():N}",
            Name = "name",
            RawValue = "v",
            ValueType = ConfigValueType.String,
        };

        var entry = await client.CreateConfigEntryAsync(service.Id, version.Id, createConfigEntryRequest);
        _entryDbHelper.Track(entry.Id);

        // Act
        var response = await client.SendDeleteConfigEntryAsync(service.Id, version.Id, entry.Id);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(content);

        var row = await _entryDbHelper.GetAsync(entry.Id);
        row.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenEntryDoesNotExist()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var version = await client.CreateConfigVersionAsync(
            service.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(version.Id);

        // Act
        var response = await client.SendDeleteConfigEntryAsync(service.Id, version.Id, Guid.NewGuid());

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();

        // Act
        var response = await client.SendDeleteConfigEntryAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _entryDbHelper.DisposeAsync();
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }
}
