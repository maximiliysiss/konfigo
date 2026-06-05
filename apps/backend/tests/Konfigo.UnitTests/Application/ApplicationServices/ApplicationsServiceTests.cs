using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.ApplicationServices;
using Konfigo.Domain.Entities;
using Konfigo.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Konfigo.UnitTests.Application.ApplicationServices;

public sealed class ApplicationsServiceTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistServiceWithNewIdAndTimestamp_WhenCalled()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        repo.AddAsync(Arg.Any<ApplicationService>(), Arg.Any<CancellationToken>())
            .Returns(c => c.Arg<ApplicationService>());
        var sut = CreateService(repo);
        var request = TestFakes.BuildCreateServiceRequest(name: "billing");

        // Act
        var result = await sut.AddAsync(request, CancellationToken.None);

        // Assert
        result.Name.Should().Be("billing");
        result.CreatedAt.Should().Be(TestFakes.Now);
        await repo.Received(1).AddAsync(result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenServiceNotFound()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        repo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.Empty<ApplicationService>());
        var sut = CreateService(repo);

        // Act
        var result = await sut.UpdateAsync(TestFakes.BuildUpdateServiceRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await repo.DidNotReceive().UpdateAsync(Arg.Any<ApplicationService>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldMutateAndPersist_WhenFound()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        var existing = TestFakes.BuildService(name: "old");
        repo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(repo);
        var request = TestFakes.BuildUpdateServiceRequest(id: existing.Id, name: "new");

        // Act
        var result = await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(existing);
        existing.Name.Should().Be("new");
        await repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNull_WhenServiceNotFound()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        repo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.Empty<ApplicationService>());
        var sut = CreateService(repo);

        // Act
        var result = await sut.DeleteAsync(TestFakes.BuildDeleteServiceRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await repo.DidNotReceive().DeleteAsync(Arg.Any<ApplicationService>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteService_WhenFound()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        var existing = TestFakes.BuildService();
        repo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(repo);

        // Act
        var result = await sut.DeleteAsync(
            TestFakes.BuildDeleteServiceRequest(id: existing.Id),
            CancellationToken.None);

        // Assert
        result.Should().BeSameAs(existing);
        await repo.Received(1).DeleteAsync(existing, Arg.Any<CancellationToken>());
    }

    private static ApplicationsService CreateService(IApplicationsRepository repo)
    {
        return new ApplicationsService(
            repo,
            new FixedDateTimeProvider(),
            NullLogger<ApplicationsService>.Instance);
    }
}
