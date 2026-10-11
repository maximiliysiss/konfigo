using System;
using FluentAssertions;
using Konfigo.Client.Exceptions;
using Konfigo.Client.Infrastructure.Validation;
using Konfigo.Client.Models;
using Xunit;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.UnitTests.Infrastructure;

public class ClassDefinitionsValidatorTests
{
    [Fact]
    public void Validate_WhenSameKeyHasSameType_ShouldNotThrow()
    {
        // Arrange
        var definitions = new[]
        {
            CreateDefinition(typeof(FirstOptions), ValueType.Number),
            CreateDefinition(typeof(SecondOptions), ValueType.Number),
        };

        // Act
        var act = () => ClassDefinitionsValidator.Validate(definitions);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenSameKeyHasDifferentTypes_ShouldThrow()
    {
        // Arrange
        var definitions = new[]
        {
            CreateDefinition(typeof(FirstOptions), ValueType.Number),
            CreateDefinition(typeof(SecondOptions), ValueType.String),
        };

        // Act
        var act = () => ClassDefinitionsValidator.Validate(definitions);

        // Assert
        act.Should()
            .Throw<ConfigKeyCollisionException>()
            .Which.Collisions.Should().ContainSingle()
            .Which.Should().Contain("Shared:Value")
            .And.Contain(nameof(FirstOptions))
            .And.Contain(nameof(SecondOptions));
    }

    private static ClassDefinition CreateDefinition(Type type, ValueType valueType)
    {
        return new ClassDefinition(
            Key: "Shared",
            Name: type.Name,
            Description: null,
            Options:
            [
                new ClassDefinition.OptionDefinition(
                    Key: "Shared:Value",
                    Name: "Value",
                    Description: null,
                    Type: valueType,
                    DefaultValue: null,
                    EnumValues: null),
            ],
            Type: type);
    }

    private sealed class FirstOptions;

    private sealed class SecondOptions;
}
