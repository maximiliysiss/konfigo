using System.Collections.Generic;
using FluentAssertions;
using Konfigo.Client.Configuration;
using Konfigo.Client.Extensions;
using Konfigo.Client.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class ConfigurationBuilderExtensionsTests
{
    [Fact]
    public void AddRealtimeConfig_ShouldNotAddSource_WhenOptionsAreMissing()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var result = builder.AddRealtimeConfig();

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().BeEmpty();
    }

    [Fact]
    public void AddRealtimeConfig_ShouldNotAddSource_WhenRealtimeConfigIsDisabled()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(RealtimeConfigOptions)}:{nameof(RealtimeConfigOptions.IsEnabled)}"] = "false",
            });

        var initialSourcesCount = builder.Sources.Count;

        // Act
        var result = builder.AddRealtimeConfig();

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().HaveCount(initialSourcesCount);
    }

    [Fact]
    public void AddRealtimeConfig_ShouldAddRealtimeConfigSource_WhenRealtimeConfigIsEnabled()
    {
        // Arrange
        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(RealtimeConfigOptions)}:{nameof(RealtimeConfigOptions.IsEnabled)}"] = "true",
            });

        // Act
        var result = builder.AddRealtimeConfig();

        // Assert
        result.Should().BeSameAs(builder);
        builder.Sources.Should().ContainSingle(source => source is RealtimeConfigSource);
    }
}
