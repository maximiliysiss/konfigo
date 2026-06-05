using System;
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
public sealed class GetConfigEntriesTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public GetConfigEntriesTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEntries()
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
            Name = "n",
            RawValue = "v",
            ValueType = ConfigValueType.String,
        };

        var entry = await client.CreateConfigEntryAsync(service.Id, version.Id, createConfigEntryRequest);

        _entryDbHelper.Track(entry.Id);

        // Act
        var entries = await client.GetConfigEntriesAsync(service.Id, version.Id);

        // Assert
        entries.Should().Contain(e => e.Id == entry.Id);
    }

    [Fact]
    public async Task GetAll_ShouldNotReturnEntries_WhenServiceIdDoesNotOwnVersion()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var owner = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(owner.Id);

        var other = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(other.Id);

        var version = await client.CreateConfigVersionAsync(
            owner.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(version.Id);

        var entry = await client.CreateConfigEntryAsync(
            owner.Id,
            version.Id,
            new CreateConfigEntryRequest
            {
                Key = $"key-{Guid.NewGuid():N}",
                Name = "n",
                RawValue = "v",
                ValueType = ConfigValueType.String,
            });
        _entryDbHelper.Track(entry.Id);

        // Act
        var entries = await client.GetConfigEntriesAsync(other.Id, version.Id);

        // Assert
        entries.Should().BeEmpty();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _entryDbHelper.DisposeAsync();
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }
}
