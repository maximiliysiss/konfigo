using FluentAssertions;
using Konfigo.Client.IntegrationTests.Shared.Fixtures;
using Konfigo.Client.IntegrationTests.Shared.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Konfigo.Client.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class CommonScenarioTests(IntegrationTestFixture fixture)
{
    [Fact]
    public void GetRequiredService_ShouldReturnOptions()
    {
        // Arrange

        // Act
        var options = fixture.Services.GetRequiredService<IOptions<SimpleOptions>>();

        // Assert
        options.Value.Value.Should().Be(42);
    }
}
