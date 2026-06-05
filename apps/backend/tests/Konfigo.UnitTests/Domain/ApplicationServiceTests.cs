using FluentAssertions;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using Xunit;

namespace Konfigo.UnitTests.Domain;

public sealed class ApplicationServiceTests
{
    [Fact]
    public void Update_ShouldMutateAllFields_WhenCalled()
    {
        // Arrange
        var service = TestFakes.BuildService(
            name: "old",
            description: "old-d",
            repositoryUrl: "old-url",
            gitLabProjectId: "1",
            contactEmail: "a@a");
        var request = new UpdateServiceRequest(
            Name: "new",
            Description: "new-d",
            RepositoryUrl: "new-url",
            GitLabProjectId: "2",
            ContactEmail: "b@b");

        // Act
        service.Update(request, TestFakes.Now);

        // Assert
        service.Name.Should().Be("new");
        service.Description.Should().Be("new-d");
        service.RepositoryUrl.Should().Be("new-url");
        service.GitLabProjectId.Should().Be("2");
        service.ContactEmail.Should().Be("b@b");
    }

    [Fact]
    public void Update_ShouldSetUpdatedAt_WhenCalled()
    {
        // Arrange
        var service = TestFakes.BuildService();
        var request = new UpdateServiceRequest("n", null, null, null, null);
        var moment = TestFakes.Now.AddHours(1);

        // Act
        service.Update(request, moment);

        // Assert
        service.UpdatedAt.Should().Be(moment);
    }
}
