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

        var configEntry = new AutoFaker<ConfigEntry>()
            .RuleFor(c => c.Timestamp, dateTime)
            .Generate();

        var provider = Create();

        // Act
        provider.Set([configEntry]);

        // Assert
        var tryGet = provider.TryGet(TimestampKey, out var value);

        tryGet.Should().BeTrue();
        value.Should().Be(dateTime.ToString(CultureInfo.InvariantCulture));
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

    private static RealtimeConfigProvider Create(ConfigEntry[]? entries = null)
        => new(VersionId.Empty, entries ?? [], NullLogger<RealtimeConfigProvider>.Instance);

    private static ConfigEntry GenerateConfigEntry()
        => AutoFaker.Generate<ConfigEntry>() with { Generation = 2, Type = ValueType.String };
}
