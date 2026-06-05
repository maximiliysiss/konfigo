using System;
using System.Collections.Generic;
using Konfigo.Abstraction.Attributes;

namespace Konfigo.Client.UnitTests.Shared.Options;

[ConfigGroup]
public class CommonOptions
{
    [ConfigKey]
    public int IntValue { get; set; }

    [ConfigKey(defaultValue: "42")]
    public int IntValueWithDef { get; set; } = 42;

    [ConfigKey]
    public string? StrValue { get; set; }

    [ConfigKey(description: "Description", defaultValue: "Default value")]
    public string StrValueWithDescAndDef { get; set; } = "Default value";

    [ConfigKey]
    public TestEnum EnumValue { get; set; }

    [ConfigKey(defaultValue: "C")]
    public TestEnum EnumValueWithDef { get; set; } = TestEnum.C;

    [ConfigKey]
    public TestEnum? NullEnumValue { get; set; }

    [ConfigKey]
    public DateTime DateTimeValue { get; set; } = DateTime.MinValue;

    [ConfigKey(defaultValue: "2022-01-01T00:00:00")]
    public DateTime DateTimeValueWithDef { get; set; } = DateTime.Parse("2022-01-01T00:00:00");

    [ConfigKey]
    public DateTimeOffset DateTimeOffsetValue { get; set; } = DateTimeOffset.MinValue;

    [ConfigKey]
    public TimeSpan TimeSpanValue { get; set; }

    [ConfigKey(defaultValue: "00:42:00")]
    public TimeSpan TimeSpanValueWithDef { get; set; } = TimeSpan.FromHours(42);

    [ConfigKey]
    public int[] ArrayValue { get; set; } = [];

    [ConfigKey(defaultValue: "[1, 2, 3]")]
    public int[] ArrayValueWithDef { get; set; } = [1, 2, 3];

    [ConfigKey]
    public Dictionary<int, int> DictionaryValue { get; set; } = [];

    [ConfigKey(defaultValue: "{\"1\": 1, \"2\": 2, \"3\": 3}")]
    public Dictionary<int, int> DictionaryValueWithDef { get; set; } = [];

    [ConfigKey]
    public bool BoolValue { get; set; } = false;

    [ConfigKey(defaultValue: "true")]
    public bool BoolValueWithDef { get; set; } = true;
}

public enum TestEnum
{
    A,
    B,
    C,
}
