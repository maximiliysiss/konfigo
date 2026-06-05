using System;
using FluentAssertions;
using Konfigo.Client.Infrastructure.Extensions;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class DateTimeOffsetsTests
{
    [Theory]
    [InlineData("2020-01-01", "2020-01-02", "2020-01-02")]
    [InlineData("2020-01-01", "2020-01-01", "2020-01-01")]
    [InlineData("2020-01-02", "2020-01-01", "2020-01-02")]
    public void Max_ShouldWorks(string a, string b, string expected)
    {
        // Arrange

        // Act
        var dateTime = DateTimeOffsets.Max(DateTime.Parse(a), DateTime.Parse(b));

        // Assert
        dateTime.Should().Be(DateTime.Parse(expected));
    }
}
