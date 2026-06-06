using System.Text.Json;
using FluentAssertions;
using Konfigo.Client.Extensions;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class JsonDocumentExtensionsTests
{
    [Fact]
    public void AsKeyValuePairs_ShouldBeEmpty_WhenKeyIsEmpty()
    {
        // Arrange
        var json = JsonDocument.Parse("{}");

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs(string.Empty);

        // Assert
        asKeyValuePairs.Should().BeEmpty();
    }
}
