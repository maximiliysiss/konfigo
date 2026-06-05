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
public sealed class CreateConfigEntryTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;
    private readonly ConfigVersionDbHelper _versionDbHelper;
    private readonly ConfigEntryDbHelper _entryDbHelper;

    public CreateConfigEntryTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
        _versionDbHelper = new ConfigVersionDbHelper(connectionFactory);
        _entryDbHelper = new ConfigEntryDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Create_ShouldCreate_WhenAdmin()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var service = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });

        _serviceDbHelper.Track(service.Id);

        var version = await client.CreateConfigVersionAsync(
            service.Id,
            new CreateConfigVersionRequest { VersionLabel = $"v-{Guid.NewGuid():N}" });

        _versionDbHelper.Track(version.Id);

        var key = $"key-{Guid.NewGuid():N}";
        var request = new CreateConfigEntryRequest
        {
            Key = key,
            Name = "Test entry",
            RawValue = "42",
            ValueType = ConfigValueType.Number,
            Description = "test description",
            GroupName = "test-group",
            GroupDescription = "test-group-description",
        };

        // Act
        var created = await client.CreateConfigEntryAsync(service.Id, version.Id, request);

        // Assert
        _entryDbHelper.Track(created.Id);

        var row = await _entryDbHelper.GetAsync(created.Id);
        row.Should().NotBeNull();
        row!.Key.Should().Be(key);
        row.Name.Should().Be(request.Name);
        row.RawValue.Should().Be(request.RawValue);
        row.ValueType.Should().Be((int)ConfigValueType.Number);
        row.GroupName.Should().Be(request.GroupName);
        row.ConfigVersionId.Should().Be(version.Id);
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_WhenCanChange()
    {
        // Arrange
        using var client = _fixture.CreateAuthenticatedClient(roles: "developer");
        var request = new CreateConfigEntryRequest
        {
            Key = $"key-{Guid.NewGuid():N}",
            Name = "Test entry",
            RawValue = "42",
            ValueType = ConfigValueType.Number,
        };

        // Act
        var response = await client.SendCreateConfigEntryAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = _fixture.CreateAnonymousClient();
        var request = new CreateConfigEntryRequest
        {
            Key = $"key-{Guid.NewGuid():N}",
            Name = "Test entry",
            RawValue = "42",
            ValueType = ConfigValueType.Number,
        };

        // Act
        var response = await client.SendCreateConfigEntryAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _entryDbHelper.DisposeAsync();
        await _versionDbHelper.DisposeAsync();
        await _serviceDbHelper.DisposeAsync();
    }
}
