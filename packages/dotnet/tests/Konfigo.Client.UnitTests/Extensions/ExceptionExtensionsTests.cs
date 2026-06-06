using System;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Client.Extensions;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class ExceptionExtensionsTests
{
    [Theory]
    [InlineData(typeof(OperationCanceledException), true)]
    [InlineData(typeof(TaskCanceledException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    public void IsCancel_ShouldReturnExpectedValue(Type exceptionType, bool expected)
    {
        // Arrange
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        // Act
        var isCancel = exception.IsCancel();

        // Assert
        isCancel.Should().Be(expected);
    }
}
