using System;
using FluentAssertions;
using Konfigo.Client.Infrastructure.Extensions;
using Xunit;

namespace Konfigo.Client.UnitTests.Infrastructure;

public class TypesTests
{
    [Theory]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(short))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(int?))]
    public void IsNumber_ShouldReturnTrue_WhenTypeIsSupportedNumber(Type type)
    {
        // Act
        var isNumber = type.IsNumber();

        // Assert
        isNumber.Should().BeTrue();
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(object))]
    public void IsNumber_ShouldReturnFalse_WhenTypeIsNotSupportedNumber(Type type)
    {
        // Act
        var isNumber = type.IsNumber();

        // Assert
        isNumber.Should().BeFalse();
    }
}
