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
public sealed class UpdateConfigEntryTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public UpdateConfigEntryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Update_ShouldUpdateRawValueAndBumpGeneration_WhenCorrectGeneration()
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
            RawValue = "1",
            ValueType = ConfigValueType.Number,
        };
        var entry = await client.CreateConfigEntryAsync(service.Id, version.Id, createConfigEntryRequest);

        _entryDbHelper.Track(entry.Id);

        var updateRequest = new UpdateConfigEntryRequest
        {
            RawValue = "100",
            Description = "updated",
            GroupName = "g2",
            GroupDescription = "g2-desc",
            Generation = entry.Generation,
        };

        // Act
        var response = await client.SendUpdateConfigEntryAsync(service.Id, version.Id, entry.Id, updateRequest);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(content);

        var row = await _entryDbHelper.GetAsync(entry.Id);
        row.Should().NotBeNull();
        row!.RawValue.Should().Be("100");
        row.Description.Should().Be("updated");
        row.GroupName.Should().Be("g2");
        row.Generation.Should().Be(entry.Generation + 1);
    }

    [Fact]
    public async Task Update_ShouldReturnEmptyBody_WhenEntryDoesNotExist()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(service.Id);

        var version = await client.CreateConfigVersionAsync(
            service.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });
        _versionDbHelper.Track(version.Id);

        var updateRequest = new UpdateConfigEntryRequest
        {
            RawValue = "100",
            Description = "updated",
            Generation = 1,
        };

        // Act
        var response = await client.SendUpdateConfigEntryAsync(service.Id, version.Id, Guid.NewGuid(), updateRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        (string.IsNullOrEmpty(content) || content == "null").Should().BeTrue();
    }

    [Fact]
    public async Task Update_ShouldThrowAndLeaveEntryUnchanged_WhenGenerationDoesNotMatch()
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
                RawValue = "1",
                ValueType = ConfigValueType.Number,
            });
        _entryDbHelper.Track(entry.Id);

        var updateRequest = new UpdateConfigEntryRequest
        {
            RawValue = "100",
            Generation = entry.Generation + 1,
        };

        // Act
        var act = () => client.SendUpdateConfigEntryAsync(service.Id, version.Id, entry.Id, updateRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Generation mismatch");

        var row = await _entryDbHelper.GetAsync(entry.Id);
        row.Should().NotBeNull();
        row!.RawValue.Should().Be("1");
        row.Generation.Should().Be(entry.Generation);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();
        var updateRequest = new UpdateConfigEntryRequest { RawValue = "100", Generation = 1 };

        // Act
        var response = await client.SendUpdateConfigEntryAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), updateRequest);

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
