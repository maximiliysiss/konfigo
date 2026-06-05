using System;
using System.Globalization;
using FluentAssertions;
using Konfigo.Client.Infrastructure.Extensions;
using Konfigo.Client.Models;
using Konfigo.Client.UnitTests.Shared.Options;
using Xunit;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.UnitTests.Extensions;

public class AssembliesTests
{
    [Fact]
    public void GetDefinitions_ShouldReturnCorrectDef()
    {
        // Arrange

        // Act
        var classDefinitions = Assemblies.GetDefinitions();

        // Assert
        var optionDefinitions = new[]
        {
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.IntValue)}",
                Name: nameof(CommonOptions.IntValue),
                Description: null,
                Type: ValueType.Number,
                DefaultValue: "0",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.IntValueWithDef)}",
                Name: nameof(CommonOptions.IntValueWithDef),
                Description: null,
                Type: ValueType.Number,
                DefaultValue: "42",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.StrValue)}",
                Name: nameof(CommonOptions.StrValue),
                Description: null,
                Type: ValueType.String,
                DefaultValue: string.Empty,
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.StrValueWithDescAndDef)}",
                Name: nameof(CommonOptions.StrValueWithDescAndDef),
                Description: "Description",
                Type: ValueType.String,
                DefaultValue: "Default value",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.EnumValue)}",
                Name: nameof(CommonOptions.EnumValue),
                Description: null,
                Type: ValueType.Enum,
                DefaultValue: "A",
                EnumValues: typeof(TestEnum).GetEnumNames()),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.EnumValueWithDef)}",
                Name: nameof(CommonOptions.EnumValueWithDef),
                Description: null,
                Type: ValueType.Enum,
                DefaultValue: "C",
                EnumValues: typeof(TestEnum).GetEnumNames()),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.NullEnumValue)}",
                Name: nameof(CommonOptions.NullEnumValue),
                Description: null,
                Type: ValueType.Enum,
                DefaultValue: null,
                EnumValues: typeof(TestEnum).GetEnumNames()),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.DateTimeValue)}",
                Name: nameof(CommonOptions.DateTimeValue),
                Description: null,
                Type: ValueType.DateTime,
                DefaultValue: DateTime.MinValue.ToString(CultureInfo.InvariantCulture),
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.DateTimeValueWithDef)}",
                Name: nameof(CommonOptions.DateTimeValueWithDef),
                Description: null,
                Type: ValueType.DateTime,
                DefaultValue: "2022-01-01T00:00:00",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.DateTimeOffsetValue)}",
                Name: nameof(CommonOptions.DateTimeOffsetValue),
                Description: null,
                Type: ValueType.DateTime,
                DefaultValue: DateTimeOffset.MinValue.ToString(CultureInfo.InvariantCulture),
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.TimeSpanValue)}",
                Name: nameof(CommonOptions.TimeSpanValue),
                Description: null,
                Type: ValueType.TimeSpan,
                DefaultValue: "00:00:00",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.TimeSpanValueWithDef)}",
                Name: nameof(CommonOptions.TimeSpanValueWithDef),
                Description: null,
                Type: ValueType.TimeSpan,
                DefaultValue: "00:42:00",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.ArrayValue)}",
                Name: nameof(CommonOptions.ArrayValue),
                Description: null,
                Type: ValueType.Array,
                DefaultValue: "[]",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.ArrayValueWithDef)}",
                Name: nameof(CommonOptions.ArrayValueWithDef),
                Description: null,
                Type: ValueType.Array,
                DefaultValue: "[1, 2, 3]",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.DictionaryValue)}",
                Name: nameof(CommonOptions.DictionaryValue),
                Description: null,
                Type: ValueType.Json,
                DefaultValue: "{}",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.DictionaryValueWithDef)}",
                Name: nameof(CommonOptions.DictionaryValueWithDef),
                Description: null,
                Type: ValueType.Json,
                DefaultValue: "{\"1\": 1, \"2\": 2, \"3\": 3}",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.BoolValue)}",
                Name: nameof(CommonOptions.BoolValue),
                Description: null,
                Type: ValueType.Boolean,
                DefaultValue: "false",
                EnumValues: null),
            new ClassDefinition.OptionDefinition(
                Key: $"{nameof(CommonOptions)}:{nameof(CommonOptions.BoolValueWithDef)}",
                Name: nameof(CommonOptions.BoolValueWithDef),
                Description: null,
                Type: ValueType.Boolean,
                DefaultValue: "true",
                EnumValues: null),
        };

        var classDefinition = new ClassDefinition(
            Key: nameof(CommonOptions),
            Type: typeof(CommonOptions),
            Name: nameof(CommonOptions),
            Description: null,
            Options: optionDefinitions);

        classDefinitions
            .Should().ContainSingle()
            .Which.Should().BeEquivalentTo(classDefinition);
    }
}
