using System;
using FluentAssertions;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using Xunit;

namespace Konfigo.UnitTests.Domain;

public sealed class ConfigEntryTests
{
    [Fact]
    public void Update_ShouldMutateFieldsAndIncrementGeneration_WhenGenerationMatches()
    {
        // Arrange
        var entry = TestFakes.BuildEntry(generation: 3, rawValue: "old");
        var request = new UpdateEntryRequest(
            RawValue: "new",
            EnumDefinition: "enum",
            Description: "d2",
            GroupName: "g2",
            GroupDescription: "gd2",
            Generation: 3);

        // Act
        entry.Update(request, TestFakes.Now);

        // Assert
        entry.RawValue.Should().Be("new");
        entry.EnumDefinition.Should().Be("enum");
        entry.Description.Should().Be("d2");
        entry.GroupName.Should().Be("g2");
        entry.GroupDescription.Should().Be("gd2");
        entry.Generation.Should().Be(4);
    }

    [Fact]
    public void Update_ShouldSetUpdatedAt_WhenSuccessful()
    {
        // Arrange
        var entry = TestFakes.BuildEntry();
        var request = new UpdateEntryRequest("v", null, null, null, null, entry.Generation);
        var moment = TestFakes.Now.AddMinutes(7);

        // Act
        entry.Update(request, moment);

        // Assert
        entry.UpdatedAt.Should().Be(moment);
    }

    [Fact]
    public void Update_ShouldThrowInvalidOperationException_WhenGenerationMismatch()
    {
        // Arrange
        var entry = TestFakes.BuildEntry(generation: 2);
        var request = new UpdateEntryRequest("v", null, null, null, null, Generation: 5);

        // Act
        var act = () => entry.Update(request, TestFakes.Now);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Generation mismatch");
        entry.Generation.Should().Be(2);
    }

    [Fact]
    public void Set_ShouldUpdateRawValueAndIncrementGeneration_WhenGenerationMatches()
    {
        // Arrange
        var entry = TestFakes.BuildEntry(generation: 1, rawValue: "old");

        // Act
        entry.Set("brand-new", TestFakes.Now, generation: 1);

        // Assert
        entry.RawValue.Should().Be("brand-new");
        entry.Generation.Should().Be(2);
    }

    [Fact]
    public void Set_ShouldSetUpdatedAt_WhenSuccessful()
    {
        // Arrange
        var entry = TestFakes.BuildEntry();
        var moment = TestFakes.Now.AddMinutes(11);

        // Act
        entry.Set("v", moment, generation: entry.Generation);

        // Assert
        entry.UpdatedAt.Should().Be(moment);
    }

    [Fact]
    public void Set_ShouldThrowInvalidOperationException_WhenGenerationMismatch()
    {
        // Arrange
        var entry = TestFakes.BuildEntry(generation: 1, rawValue: "old");

        // Act
        var act = () => entry.Set("nope", TestFakes.Now, generation: 99);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Generation mismatch");
        entry.RawValue.Should().Be("old");
        entry.Generation.Should().Be(1);
    }
}
