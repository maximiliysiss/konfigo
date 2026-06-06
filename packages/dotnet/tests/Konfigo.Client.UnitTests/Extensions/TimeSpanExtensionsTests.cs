using System;
using FluentAssertions;
using Konfigo.Client.Extensions;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class TimeSpanExtensionsTests
{
    [Fact]
    public void Jitter_ShouldAddRandomMillisecondsInsideConfiguredRange()
    {
        // Arrange
        var timeSpan = TimeSpan.FromSeconds(1);

        // Act
        var jittered = timeSpan.Jitter(min: 10, max: 11);

        // Assert
        jittered.Should().Be(timeSpan + TimeSpan.FromMilliseconds(10));
    }
}
