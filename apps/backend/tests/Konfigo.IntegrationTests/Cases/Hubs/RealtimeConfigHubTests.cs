using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.IntegrationTests.Shared.Extensions;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.Hubs;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RealtimeConfigHubTests
{
    private const string NegotiateUrl = "/hubs/config/negotiate?negotiateVersion=1";

    private readonly IntegrationTestFixture _fixture;

    public RealtimeConfigHubTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Negotiate_ShouldReturnOk_WhenCanChange()
    {
        // Arrange
        using var client = _fixture.CreateAuthenticatedClient(roles: "developer");

        // Act
        var response = await client.PostAsync(NegotiateUrl, content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Negotiate_ShouldReturnOk_WhenCanAll()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        // Act
        var response = await client.PostAsync(NegotiateUrl, content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Negotiate_ShouldReturnForbidden_WhenAuthenticatedWithoutCanChange()
    {
        // Arrange
        using var client = _fixture.CreateUserClient();

        // Act
        var response = await client.PostAsync(NegotiateUrl, content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Negotiate_ShouldReturnUnauthorized_WhenAnonymous()
    {
        // Arrange
        using var client = _fixture.CreateAnonymousClient();

        // Act
        var response = await client.PostAsync(NegotiateUrl, content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
