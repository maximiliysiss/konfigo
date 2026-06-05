using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.IntegrationTests.Shared.Extensions;
using Konfigo.IntegrationTests.Shared.Fixtures;
using Xunit;

namespace Konfigo.IntegrationTests.Cases.Auth;

[Collection(nameof(IntegrationTestCollection))]
public sealed class AuthTests
{
    private readonly IntegrationTestFixture _fixture;

    public AuthTests(IntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Me_ShouldReturnUser_WhenAuthenticated()
    {
        // Arrange
        using var client = _fixture.CreateAdminClient();

        // Act
        var me = await client.GetMeAsync();

        // Assert
        me.Id.Should().NotBeNullOrEmpty();
        me.Roles.Should().Contain("admin");
        me.Permissions.Should().Contain(["canAll", "canChange"]);
    }

    [Fact]
    public async Task Me_ShouldReturnNoContent_WhenAnonymous()
    {
        // Arrange
        using var client = _fixture.CreateAnonymousClient();

        // Act
        var response = await client.SendGetMeAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
