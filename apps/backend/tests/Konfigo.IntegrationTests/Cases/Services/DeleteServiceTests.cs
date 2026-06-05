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
public sealed class DeleteServiceTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;

    public DeleteServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
    }

    [Fact]
    public async Task Delete_ShouldRemoveFromDb_WhenAdmin()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        var created = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = $"svc-{Guid.NewGuid():N}" });
        _serviceDbHelper.Track(created.Id);

        // Act
        var response = await client.SendDeleteServiceAsync(created.Id);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.Should().BeTrue(content);

        var row = await _serviceDbHelper.GetAsync(created.Id);
        row.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenServiceDoesNotExist()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        // Act
        var response = await client.SendDeleteServiceAsync(Guid.NewGuid());

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();

        // Act
        var response = await client.SendDeleteServiceAsync(Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _serviceDbHelper.DisposeAsync();
}
