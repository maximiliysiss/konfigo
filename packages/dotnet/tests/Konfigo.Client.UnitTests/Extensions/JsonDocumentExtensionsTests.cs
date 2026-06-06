using System.Text.Json;
using FluentAssertions;
using Konfigo.Client.Extensions;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class JsonDocumentExtensionsTests
{
    [Fact]
    public void AsKeyValuePairs_ShouldBeEmpty_WhenObjectIsEmpty()
    {
        // Arrange
        var json = JsonDocument.Parse("{}");

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs(string.Empty);

        // Assert
        asKeyValuePairs.Should().BeEmpty();
    }

    [Fact]
    public void AsKeyValuePairs_ShouldBeEmpty_WhenArrayIsEmpty()
    {
        // Arrange
        var json = JsonDocument.Parse("[]");

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs("Root");

        // Assert
        asKeyValuePairs.Should().BeEmpty();
    }

    [Theory]
    [InlineData("\"value\"")]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    public void AsKeyValuePairs_ShouldBeEmpty_WhenRootElementIsNotObjectOrArray(string jsonString)
    {
        // Arrange
        var json = JsonDocument.Parse(jsonString);

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs("Root");

        // Assert
        asKeyValuePairs.Should().BeEmpty();
    }

    [Fact]
    public void AsKeyValuePairs_ShouldReturnProperties_WhenObjectContainsStringAndNullValues()
    {
        // Arrange
        var json = JsonDocument.Parse(
            """
            {
              "StringValue": "value",
              "NullValue": null
            }
            """);

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs("Root");

        // Assert
        asKeyValuePairs.Should().BeEquivalentTo(
        [
            ("Root:StringValue", "value"),
            ("Root:NullValue", null)
        ]);
    }

    [Fact]
    public void AsKeyValuePairs_ShouldReturnRawTextValues_WhenObjectContainsNumberAndBooleanValues()
    {
        // Arrange
        var json = JsonDocument.Parse(
            """
            {
              "IntegerValue": 42,
              "DecimalValue": 3.14,
              "TrueValue": true,
              "FalseValue": false
            }
            """);

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs("Root");

        // Assert
        asKeyValuePairs.Should().BeEquivalentTo(
        [
            ("Root:IntegerValue", "42"),
            ("Root:DecimalValue", "3.14"),
            ("Root:TrueValue", "true"),
            ("Root:FalseValue", "false")
        ]);
    }

    [Fact]
    public void AsKeyValuePairs_ShouldReturnIndexedValues_WhenArrayContainsStringAndNullValues()
    {
        // Arrange
        var json = JsonDocument.Parse("""["first", null, "third"]""");

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs("Root");

        // Assert
        asKeyValuePairs.Should().BeEquivalentTo(
        [
            ("Root:0", "first"),
            ("Root:1", null),
            ("Root:2", "third")
        ]);
    }

    [Fact]
    public void AsKeyValuePairs_ShouldReturnIndexedRawTextValues_WhenArrayContainsNumberAndBooleanValues()
    {
        // Arrange
        var json = JsonDocument.Parse("""[42, 3.14, true, false]""");

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs("Root");

        // Assert
        asKeyValuePairs.Should().BeEquivalentTo(
        [
            ("Root:0", "42"),
            ("Root:1", "3.14"),
            ("Root:2", "true"),
            ("Root:3", "false")
        ]);
    }

    [Fact]
    public void AsKeyValuePairs_ShouldReturnNestedProperties_WhenObjectContainsObjectsAndArrays()
    {
        // Arrange
        var json = JsonDocument.Parse(
            """
            {
              "Object": {
                "Child": "value"
              },
              "Array": [
                "first",
                {
                  "Child": "second"
                },
                [
                  "third"
                ]
              ]
            }
            """);

        // Act
        var asKeyValuePairs = json.AsKeyValuePairs("Root");

        // Assert
        asKeyValuePairs.Should().BeEquivalentTo(
        [
            ("Root:Object:Child", "value"),
            ("Root:Array:0", "first"),
            ("Root:Array:1:Child", "second"),
            ("Root:Array:2:0", "third")
        ]);
    }
}
