using System;
using System.Globalization;
using AutoBogus;
using FluentAssertions;
using Konfigo.Client.Configuration;
using Konfigo.Client.Entities;
using Konfigo.Client.Models;
using Konfigo.Client.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.UnitTests.Configuration;

public class RealtimeConfigProviderTests
{
    private const string TimestampKey = $"{nameof(RealtimeConfigOptions)}:{nameof(RealtimeConfigOptions.Timestamp)}";
    private const string VersionIdKey = $"{nameof(RealtimeConfigOptions)}:{nameof(RealtimeConfigOptions.VersionId)}";

    [Fact]
    public void Load_ShouldSetInitialConfig()
    {
        // Arrange
        var configEntry = GenerateConfigEntry();

        var provider = Create([configEntry]);

        // Act
        provider.Load();

        // Assert
        provider.TryGet(configEntry.Key, out var value).Should().BeTrue();
        value.Should().Be(configEntry.Value);
    }

    [Fact]
    public void Load_ShouldSetVersionId()
    {
        // Arrange
        var versionId = Guid.NewGuid().ToString();
        var provider = Create(versionId: new VersionId(versionId));

        // Act
        provider.Load();

        // Assert
        provider.TryGet(VersionIdKey, out var value).Should().BeTrue();
        value.Should().Be(versionId);
    }

    [Fact]
    public void Set_ShouldSetConfig()
    {
        // Arrange
        var configEntry = GenerateConfigEntry();

        var provider = Create();

        // Act
        provider.Set([configEntry]);

        // Assert
        var tryGet = provider.TryGet(configEntry.Key, out var value);

        tryGet.Should().BeTrue();
        value.Should().Be(configEntry.Value);
    }

    [Fact]
    public void Set_ShouldReload_WhenConfigWasUpdated()
    {
        // Arrange
        var configEntry = GenerateConfigEntry();

        var provider = Create();

        var isReloaded = false;
        using var _ = provider.GetReloadToken().RegisterChangeCallback(_ => isReloaded = true, null);

        // Act
        provider.Set([configEntry]);

        // Assert
        isReloaded.Should().BeTrue();
    }

    [Fact]
    public void Set_ShouldNotReload_WhenConfigWasNotUpdated()
    {
        // Arrange
        var configEntry = GenerateConfigEntry();
        var staleConfigEntry = configEntry with { Generation = configEntry.Generation - 1, Value = "New value" };

        var provider = Create();

        provider.Set([configEntry]);

        var isReloaded = false;
        using var _ = provider.GetReloadToken().RegisterChangeCallback(_ => isReloaded = true, null);

        // Act
        provider.Set([staleConfigEntry]);

        // Assert
        isReloaded.Should().BeFalse();
    }

    [Fact]
    public void Set_ShouldOverride_WhenGenerationIsGreater()
    {
        // Arrange
        var configEntry = GenerateConfigEntry();
        var newConfigEntry = configEntry with { Generation = configEntry.Generation + 1, Value = "New value" };

        var provider = Create();

        // Act
        provider.Set([configEntry]);
        provider.Set([newConfigEntry]);

        // Assert
        var tryGet = provider.TryGet(configEntry.Key, out var value);

        tryGet.Should().BeTrue();
        value.Should().Be(newConfigEntry.Value);
    }

    [Fact]
    public void Set_ShouldUpdateTimestamp_WhenNew()
    {
        // Arrange
        var dateTime = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        var configEntry = GenerateConfigEntry() with { Timestamp = dateTime };

        var provider = Create();

        // Act
        provider.Set([configEntry]);

        // Assert
        var tryGet = provider.TryGet(TimestampKey, out var value);

        tryGet.Should().BeTrue();
        value.Should().Be(dateTime.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Set_ShouldKeepLatestTimestamp_WhenNextGenerationHasOlderTimestamp()
    {
        // Arrange
        var latest = new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero);
        var older = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        var configEntry = GenerateConfigEntry() with { Generation = 2, Timestamp = latest };
        var nextGenerationConfigEntry = configEntry with { Generation = 3, Value = "New value", Timestamp = older };

        var provider = Create();

        // Act
        provider.Set([configEntry]);
        provider.Set([nextGenerationConfigEntry]);

        // Assert
        provider.TryGet(TimestampKey, out var value).Should().BeTrue();
        value.Should().Be(latest.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Set_ShouldDoNotOverride_WhenGenerationIsLower()
    {
        // Arrange
        var configEntry = GenerateConfigEntry();
        var newConfigEntry = configEntry with { Generation = configEntry.Generation - 1, Value = "New value" };

        var provider = Create();

        // Act
        provider.Set([configEntry]);
        provider.Set([newConfigEntry]);

        // Assert
        var tryGet = provider.TryGet(configEntry.Key, out var value);

        tryGet.Should().BeTrue();
        value.Should().Be(configEntry.Value);
    }

    [Fact]
    public void Set_ShouldDoNotOverride_WhenGenerationIsEqual()
    {
        // Arrange
        var configEntry = GenerateConfigEntry();
        var newConfigEntry = configEntry with { Value = "New value" };

        var provider = Create();

        // Act
        provider.Set([configEntry]);
        provider.Set([newConfigEntry]);

        // Assert
        var tryGet = provider.TryGet(configEntry.Key, out var value);

        tryGet.Should().BeTrue();
        value.Should().Be(configEntry.Value);
    }

    [Fact]
    public void Set_ShouldUnwindJsonObject()
    {
        // Arrange
        var configEntry = GenerateConfigEntry() with
        {
            Key = "Root",
            Type = ValueType.Json,
            Value =
                """
                {
                  "StringValue": "value",
                  "Nested": {
                    "NumberValue": 42
                  },
                  "ArrayValue": [
                    "first",
                    "second"
                  ]
                }
                """,
        };

        var provider = Create();

        // Act
        provider.Set([configEntry]);

        // Assert
        provider.TryGet("Root:StringValue", out var stringValue).Should().BeTrue();
        stringValue.Should().Be("value");

        provider.TryGet("Root:Nested:NumberValue", out var numberValue).Should().BeTrue();
        numberValue.Should().Be("42");

        provider.TryGet("Root:ArrayValue:0", out var firstArrayValue).Should().BeTrue();
        firstArrayValue.Should().Be("first");

        provider.TryGet("Root:ArrayValue:1", out var secondArrayValue).Should().BeTrue();
        secondArrayValue.Should().Be("second");
    }

    [Fact]
    public void Set_ShouldUnwindArray()
    {
        // Arrange
        var configEntry = GenerateConfigEntry() with
        {
            Key = "Root",
            Type = ValueType.Array,
            Value =
                """
                [
                  "first",
                  {
                    "Child": true
                  }
                ]
                """,
        };

        var provider = Create();

        // Act
        provider.Set([configEntry]);

        // Assert
        provider.TryGet("Root:0", out var firstValue).Should().BeTrue();
        firstValue.Should().Be("first");

        provider.TryGet("Root:1:Child", out var childValue).Should().BeTrue();
        childValue.Should().Be("true");
    }

    [Theory]
    [InlineData((int)ValueType.Json, "{}")]
    [InlineData((int)ValueType.Array, "[]")]
    public void Set_ShouldSetEmptyString_WhenStructuredValueIsEmpty(int type, string value)
    {
        // Arrange
        var configEntry = GenerateConfigEntry() with
        {
            Key = "Root",
            Type = (ValueType)type,
            Value = value,
        };

        var provider = Create();

        // Act
        provider.Set([configEntry]);

        // Assert
        provider.TryGet("Root", out var actual).Should().BeTrue();
        actual.Should().BeEmpty();
    }

    [Fact]
    public void Set_ShouldIgnoreStaleStructuredValue()
    {
        // Arrange
        var configEntry = GenerateConfigEntry() with
        {
            Key = "Root",
            Type = ValueType.Json,
            Generation = 2,
            Value = """{"Child":"current"}""",
        };

        var staleConfigEntry = configEntry with
        {
            Generation = 1,
            Value = """{"Child":"stale"}""",
        };

        var provider = Create();

        // Act
        provider.Set([configEntry]);
        provider.Set([staleConfigEntry]);

        // Assert
        provider.TryGet("Root:Child", out var value).Should().BeTrue();
        value.Should().Be("current");
    }

    private static RealtimeConfigProvider Create(ConfigEntry[]? entries = null, VersionId? versionId = null)
        => new(versionId ?? VersionId.Empty, entries ?? [], NullLogger<RealtimeConfigProvider>.Instance);

    private static ConfigEntry GenerateConfigEntry()
        => AutoFaker.Generate<ConfigEntry>() with { Generation = 2, Type = ValueType.String };
}
