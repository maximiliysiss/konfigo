using FluentAssertions;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using Xunit;

namespace Konfigo.UnitTests.Domain;

public sealed class ConfigVersionTests
{
    [Fact]
    public void Update_ShouldMutateFields_WhenCalled()
    {
        // Arrange
        var version = TestFakes.BuildVersion(versionLabel: "v1", description: "desc1");
        var request = new UpdateVersionRequest(VersionLabel: "v2", Description: "desc2");

        // Act
        version.Update(request, TestFakes.Now);

        // Assert
        version.VersionLabel.Should().Be("v2");
        version.Description.Should().Be("desc2");
    }

    [Fact]
    public void Update_ShouldSetUpdatedAt_WhenCalled()
    {
        // Arrange
        var version = TestFakes.BuildVersion();
        var request = new UpdateVersionRequest(VersionLabel: "v2", Description: null);
        var moment = TestFakes.Now.AddMinutes(3);

        // Act
        version.Update(request, moment);

        // Assert
        version.UpdatedAt.Should().Be(moment);
    }
}
