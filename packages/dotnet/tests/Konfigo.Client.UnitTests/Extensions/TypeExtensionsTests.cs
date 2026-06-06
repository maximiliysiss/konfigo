using System;
using FluentAssertions;
using Konfigo.Client.Extensions;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class TypeExtensionsTests
{
    [Fact]
    public void HasCustomAttribute_ShouldReturnTrue_WhenTypeHasAttribute()
    {
        // Act
        var hasAttribute = typeof(AnnotatedOptions).HasCustomAttribute<TestAttribute>();

        // Assert
        hasAttribute.Should().BeTrue();
    }

    [Fact]
    public void HasCustomAttribute_ShouldReturnFalse_WhenTypeDoesNotHaveAttribute()
    {
        // Act
        var hasAttribute = typeof(PlainOptions).HasCustomAttribute<TestAttribute>();

        // Assert
        hasAttribute.Should().BeFalse();
    }

    [Fact]
    public void HasCustomAttribute_ShouldReturnTrue_WhenPropertyHasAttribute()
    {
        // Arrange
        var property = typeof(AnnotatedOptions).GetProperty(nameof(AnnotatedOptions.Value))!;

        // Act
        var hasAttribute = property.HasCustomAttribute<TestAttribute>();

        // Assert
        hasAttribute.Should().BeTrue();
    }

    [Fact]
    public void HasCustomAttribute_ShouldReturnFalse_WhenPropertyDoesNotHaveAttribute()
    {
        // Arrange
        var property = typeof(PlainOptions).GetProperty(nameof(PlainOptions.Value))!;

        // Act
        var hasAttribute = property.HasCustomAttribute<TestAttribute>();

        // Assert
        hasAttribute.Should().BeFalse();
    }

    [Test]
    private sealed class AnnotatedOptions
    {
        [Test]
        public int Value { get; set; }
    }

    private sealed class PlainOptions
    {
        public int Value { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    private sealed class TestAttribute : Attribute;
}
