using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.ApplicationServices;
using Konfigo.Application.Services.ApplicationServices.Models;
using Konfigo.Application.Services.ApplicationServices.Options;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using Medallion.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

    [Fact]
    public async Task AddMemberAsync_ShouldAddMemberAndPersist_WhenServiceExists()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        var existing = TestFakes.BuildService();
        var userId = new UserId("user-1");
        repo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(repo);

        // Act
        var result = await sut.AddMemberAsync(
            new AddMemberRequest(existing.Id, userId, new User(new UserId(Guid.NewGuid().ToString()), "Test User", "admin")),
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existing.Members.Should().Contain(userId);
        existing.UpdatedAt.Should().Be(TestFakes.Now);
        await repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddMemberAsync_ShouldReturnFalseAndSkipPersist_WhenMemberAlreadyExists()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        var userId = new UserId("user-1");
        var existing = TestFakes.BuildService();
        existing.Members.Add(userId);
        repo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(repo);

        // Act
        var result = await sut.AddMemberAsync(
            new AddMemberRequest(existing.Id, userId, new User(new UserId(Guid.NewGuid().ToString()), "Test User", "admin")),
            CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        existing.Members.Should().ContainSingle(x => x == userId);
        await repo.DidNotReceive().UpdateAsync(Arg.Any<ApplicationService>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveMemberAsync_ShouldRemoveMemberAndPersist_WhenServiceExists()
    {
        // Arrange
        var repo = Substitute.For<IApplicationsRepository>();
        var userId = new UserId("user-1");
        var existing = TestFakes.BuildService();
        existing.Members.Add(userId);
        repo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(repo);

        // Act
        var result = await sut.RemoveMemberAsync(
            new RemoveMemberRequest(existing.Id, userId, new User(new UserId(Guid.NewGuid().ToString()), "Test User", "admin")),
            CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existing.Members.Should().NotContain(userId);
        existing.UpdatedAt.Should().Be(TestFakes.Now);
        await repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    private static ApplicationsService CreateService(IApplicationsRepository repo)
    {
        var factory = Substitute.For<IDistributedLockProvider>();

        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLock
            .TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new Disposable());

        factory
            .CreateLock(Arg.Any<string>())
            .Returns(distributedLock);

        return new ApplicationsService(
            repo,
            new FixedDateTimeProvider(),
            NullLogger<ApplicationsService>.Instance,
            factory,
            Options.Create(new ApplicationsServiceOptions { LockTimeout = TimeSpan.FromSeconds(5) }));
    }

    private sealed class Disposable : IDistributedSynchronizationHandle
    {
        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public CancellationToken HandleLostToken => CancellationToken.None;
    }
}
