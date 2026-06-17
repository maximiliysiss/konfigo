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
public sealed class GetServiceTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ApplicationServiceDbHelper _serviceDbHelper;

    public GetServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var connectionFactory = fixture.Services.GetRequiredService<IConnectionFactory>();
        _serviceDbHelper = new ApplicationServiceDbHelper(connectionFactory);
    }

    [Fact]
    public async Task GetById_ShouldReturnService_WhenExists()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();
        var name = $"svc-{Guid.NewGuid():N}";

        var created = await client.CreateServiceAsync(new CreateOrUpdateServiceRequest { Name = name });
        _serviceDbHelper.Track(created.Id);

        await client.AddMemberAsync(created.Id, Guid.NewGuid());

        // Act
        var service = await client.GetServiceAsync(created.Id);

        // Assert
        service.Should().NotBeNull();
        service!.Id.Should().Be(created.Id);
        service.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenNotExists()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        // Act
        var response = await client.SendGetServiceAsync(Guid.NewGuid());

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();

        // Act
        var response = await client.SendGetServiceAsync(Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _serviceDbHelper.DisposeAsync();
}
