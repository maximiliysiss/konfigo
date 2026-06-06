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
using Konfigo.IntegrationTests.Shared.Responses;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.ConfigEntries;

[Collection(nameof(IntegrationTestCollection))]
public sealed class SetConfigEntriesTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public SetConfigEntriesTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Set_ShouldUpdateRawValuesInBatch()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var createConfigVersionRequest = new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" };
        var version = await client.CreateConfigVersionAsync(service.Id, createConfigVersionRequest);
        _versionDbHelper.Track(version.Id);

        var createConfigEntryRequest = new CreateConfigEntryRequest
        {
            Key = $"key-{Guid.NewGuid():N}",
            Name = "name",
            RawValue = "init",
            ValueType = ConfigValueType.String,
        };
        var entry1 = await client.CreateConfigEntryAsync(service.Id, version.Id, createConfigEntryRequest);
        _entryDbHelper.Track(entry1.Id);

        var configEntryRequest = new CreateConfigEntryRequest
        {
            Key = $"key-{Guid.NewGuid():N}",
            Name = "name",
            RawValue = "init",
            ValueType = ConfigValueType.String,
        };
        var entry2 = await client.CreateConfigEntryAsync(service.Id, version.Id, configEntryRequest);
        _entryDbHelper.Track(entry2.Id);

        var setRequest = new[]
        {
            new SetConfigEntryRequest { Id = entry1.Id, RawValue = "v1", Generation = entry1.Generation },
            new SetConfigEntryRequest { Id = entry2.Id, RawValue = "v2", Generation = entry2.Generation },
        };

        // Act
        var response = await client.SendSetConfigEntriesAsync(service.Id, version.Id, setRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(content);

        var row1 = await _entryDbHelper.GetAsync(entry1.Id);
        var row2 = await _entryDbHelper.GetAsync(entry2.Id);
        row1.Should().NotBeNull();
        row2.Should().NotBeNull();
        row1!.RawValue.Should().Be("v1");
        row2!.RawValue.Should().Be("v2");
        row1.Generation.Should().Be(entry1.Generation + 1);
        row2.Generation.Should().Be(entry2.Generation + 1);
    }

    [Fact]
    public async Task Set_ShouldUpdateRawValue_WhenAdminHasNoServiceClaims()
    {
        // Arrange
        using var admin = _fixture.CreateAdminClient();

        var service = await admin.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var version = await admin.CreateConfigVersionAsync(
            service.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(version.Id);

        var entry = await admin.CreateConfigEntryAsync(
            service.Id,
            version.Id,
            new CreateConfigEntryRequest
            {
                Key = $"key-{Guid.NewGuid():N}",
                Name = "name",
                RawValue = "init",
                ValueType = ConfigValueType.String,
            });
        _entryDbHelper.Track(entry.Id);

        using var client = _fixture.CreateAuthenticatedClient(roles: "admin", services: string.Empty);
        var setRequest = new[]
        {
            new SetConfigEntryRequest { Id = entry.Id, RawValue = "changed", Generation = entry.Generation },
        };

        // Act
        var response = await client.SendSetConfigEntriesAsync(service.Id, version.Id, setRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(content);

        var row = await _entryDbHelper.GetAsync(entry.Id);
        row.Should().NotBeNull();
        row!.RawValue.Should().Be("changed");
        row.Generation.Should().Be(entry.Generation + 1);
    }

    [Fact]
    public async Task Set_ShouldReturnForbidden_WhenCanChangeHasNoServiceClaims()
    {
        // Arrange
        using var admin = _fixture.CreateAdminClient();

        var service = await admin.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var version = await admin.CreateConfigVersionAsync(
            service.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(version.Id);

        var entry = await admin.CreateConfigEntryAsync(
            service.Id,
            version.Id,
            new CreateConfigEntryRequest
            {
                Key = $"key-{Guid.NewGuid():N}",
                Name = "name",
                RawValue = "init",
                ValueType = ConfigValueType.String,
            });
        _entryDbHelper.Track(entry.Id);

        using var client = _fixture.CreateAuthenticatedClient(roles: "developer", services: "other-service");
        var setRequest = new[]
        {
            new SetConfigEntryRequest { Id = entry.Id, RawValue = "changed", Generation = entry.Generation },
        };

        // Act
        var response = await client.SendSetConfigEntriesAsync(service.Id, version.Id, setRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);

        var row = await _entryDbHelper.GetAsync(entry.Id);
        row.Should().NotBeNull();
        row!.RawValue.Should().Be("init");
        row.Generation.Should().Be(entry.Generation);
    }

    [Fact]
    public async Task Set_ShouldReturnEmptyArrayAndLeaveEntriesUnchanged_WhenAnyEntryDoesNotExist()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var version = await client.CreateConfigVersionAsync(
            service.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(version.Id);

        var entry = await client.CreateConfigEntryAsync(
            service.Id,
            version.Id,
            new CreateConfigEntryRequest
            {
                Key = $"key-{Guid.NewGuid():N}",
                Name = "name",
                RawValue = "init",
                ValueType = ConfigValueType.String,
            });
        _entryDbHelper.Track(entry.Id);

        var setRequest = new[]
        {
            new SetConfigEntryRequest { Id = entry.Id, RawValue = "changed", Generation = entry.Generation },
            new SetConfigEntryRequest { Id = Guid.NewGuid(), RawValue = "missing", Generation = 1 },
        };

        // Act
        var response = await client.SendSetConfigEntriesAsync(service.Id, version.Id, setRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var entries = await response.ReadRequiredKonfigoJsonAsync<EntryResponse[]>();
        entries.Should().BeEmpty();

        var row = await _entryDbHelper.GetAsync(entry.Id);
        row.Should().NotBeNull();
        row!.RawValue.Should().Be("init");
        row.Generation.Should().Be(entry.Generation);
    }

    [Fact]
    public async Task Set_ShouldThrowAndLeaveEntriesUnchanged_WhenGenerationDoesNotMatch()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var version = await client.CreateConfigVersionAsync(
            service.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(version.Id);

        var entry = await client.CreateConfigEntryAsync(
            service.Id,
            version.Id,
            new CreateConfigEntryRequest
            {
                Key = $"key-{Guid.NewGuid():N}",
                Name = "name",
                RawValue = "init",
                ValueType = ConfigValueType.String,
            });
        _entryDbHelper.Track(entry.Id);

        var setRequest = new[]
        {
            new SetConfigEntryRequest { Id = entry.Id, RawValue = "changed", Generation = entry.Generation + 1 },
        };

        // Act
        var act = () => client.SendSetConfigEntriesAsync(service.Id, version.Id, setRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Generation mismatch");

        var row = await _entryDbHelper.GetAsync(entry.Id);
        row.Should().NotBeNull();
        row!.RawValue.Should().Be("init");
        row.Generation.Should().Be(entry.Generation);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _entryDbHelper.DisposeAsync();
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }
}
