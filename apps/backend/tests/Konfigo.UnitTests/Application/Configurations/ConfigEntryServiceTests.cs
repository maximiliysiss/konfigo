using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Application.Services.Configurations.Options;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using Medallion.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Konfigo.UnitTests.Application.Configurations;

public sealed class ConfigEntryServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistEntryWithNewIdAndTimestamp_WhenCalled()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();
        var sut = CreateService(entryRepo, serviceRepo);
        var request = TestFakes.BuildCreateEntryRequest();

        // Act
        var result = await sut.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Key.Should().Be(request.Key);
        result.Name.Should().Be(request.Name);
        result.RawValue.Should().Be(request.RawValue);
        result.ConfigVersionId.Should().Be(request.VersionId);
        result.CreatedAt.Should().Be(TestFakes.Now);
        result.Generation.Should().Be(1);
        await entryRepo.Received(1).AddAsync(result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenEntryNotFound()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();
        entryRepo.GetAsync(Arg.Any<SearchEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.Empty<ConfigEntry>());

        var sut = CreateService(entryRepo, serviceRepo);

        // Act
        var result = await sut.UpdateAsync(TestFakes.BuildUpdateEntryRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await entryRepo.DidNotReceive().UpdateAsync(Arg.Any<ConfigEntry[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndPersist_WhenEntryFound()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();

        var existing = TestFakes.BuildEntry(rawValue: "old", generation: 1);
        entryRepo.GetAsync(Arg.Any<SearchEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(entryRepo, serviceRepo);
        var request = TestFakes.BuildUpdateEntryRequest(id: existing.Id, rawValue: "new", generation: 1);

        // Act
        var result = await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(existing);
        existing.RawValue.Should().Be("new");
        existing.Generation.Should().Be(2);
        await entryRepo.Received(1).UpdateAsync(
            Arg.Is<ConfigEntry[]>(a => a.Length == 1 && a[0] == existing),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldAcquireLockUsingComposedKey_WhenCalled()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();

        var lockFactory = Substitute.For<IDistributedLockProvider>();

        var distributedLock = Substitute.For<IDistributedLock>();

        distributedLock
            .AcquireAsync(Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(new Disposable());
        distributedLock
            .TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new Disposable());

        lockFactory
            .CreateLock(Arg.Any<string>())
            .Returns(distributedLock);

        entryRepo.GetAsync(Arg.Any<SearchEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.Empty<ConfigEntry>());

        var sut = CreateService(entryRepo, serviceRepo, lockFactory: lockFactory);
        var request = TestFakes.BuildUpdateEntryRequest();

        // Act
        await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        var expectedKey = $"{request.ServiceId}::{request.VersionId}::entry";
        lockFactory.Received(1).CreateLock(expectedKey);
    }

    [Fact]
    public async Task SetAsync_ShouldReturnEmpty_WhenServiceNotFound()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();
        serviceRepo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.Empty<ApplicationService>());

        var sut = CreateService(entryRepo, serviceRepo);
        var request = new SetEntryRequest(
            ServiceId.New(),
            VersionId.New(),
            [new SetEntryRequest.SetRequest(EntryId.New(), "v", 1)],
            new UserId(Guid.NewGuid().ToString()));

        // Act
        var result = await sut.SetAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        await entryRepo.DidNotReceive().UpdateAsync(Arg.Any<ConfigEntry[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_ShouldThrowUnauthorizedAccess_WhenUserRoleMissingServiceName()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();

        var service = TestFakes.BuildService(name: "payments");
        serviceRepo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(service));

        var sut = CreateService(entryRepo, serviceRepo);
        var request = new SetEntryRequest(
            service.Id,
            VersionId.New(),
            [new SetEntryRequest.SetRequest(EntryId.New(), "v", 1)],
            new UserId(Guid.NewGuid().ToString()));

        // Act
        var act = () => sut.SetAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SetAsync_ShouldReturnEmpty_WhenEntryCountMismatchesRequest()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();

        var service = TestFakes.BuildService(name: "billing");
        var updatedBy = new UserId(Guid.NewGuid().ToString());
        service.Members.Add(updatedBy);
        serviceRepo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(service));

        // request asks for 2 ids but repo returns only 1
        var existing = TestFakes.BuildEntry();
        entryRepo.GetAsync(Arg.Any<SearchEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(entryRepo, serviceRepo);
        var request = new SetEntryRequest(
            service.Id,
            VersionId.New(),
            [
                new SetEntryRequest.SetRequest(existing.Id, "v1", 1),
                new SetEntryRequest.SetRequest(EntryId.New(), "v2", 1),
            ],
            updatedBy);

        // Act
        var result = await sut.SetAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        await entryRepo.DidNotReceive().UpdateAsync(Arg.Any<ConfigEntry[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_ShouldUpdateAllEntries_WhenAllFound()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();

        var service = TestFakes.BuildService(name: "billing");
        var updatedBy = new UserId(Guid.NewGuid().ToString());
        service.Members.Add(updatedBy);
        serviceRepo.GetAsync(Arg.Any<SearchServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(service));

        var entry1 = TestFakes.BuildEntry(rawValue: "old1", generation: 1);
        var entry2 = TestFakes.BuildEntry(rawValue: "old2", generation: 1);
        entryRepo.GetAsync(Arg.Any<SearchEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.FromMany(entry1, entry2));

        var sut = CreateService(entryRepo, serviceRepo);
        var request = new SetEntryRequest(
            service.Id,
            VersionId.New(),
            [
                new SetEntryRequest.SetRequest(entry1.Id, "new1", 1),
                new SetEntryRequest.SetRequest(entry2.Id, "new2", 1),
            ],
            updatedBy);

        // Act
        var result = await sut.SetAsync(request, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        entry1.RawValue.Should().Be("new1");
        entry2.RawValue.Should().Be("new2");
        entry1.Generation.Should().Be(2);
        entry2.Generation.Should().Be(2);
        await entryRepo.Received(1).UpdateAsync(
            Arg.Is<ConfigEntry[]>(a => a.Length == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();
        entryRepo.GetAsync(Arg.Any<SearchEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.Empty<ConfigEntry>());

        var sut = CreateService(entryRepo, serviceRepo);

        // Act
        var result = await sut.DeleteAsync(TestFakes.BuildDeleteEntryRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await entryRepo.DidNotReceive().DeleteAsync(Arg.Any<ConfigEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeletePersistedEntry_WhenFound()
    {
        // Arrange
        var entryRepo = Substitute.For<IConfigEntryRepository>();
        var serviceRepo = Substitute.For<IApplicationsRepository>();

        var existing = TestFakes.BuildEntry();
        entryRepo.GetAsync(Arg.Any<SearchEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(entryRepo, serviceRepo);

        // Act
        var result = await sut.DeleteAsync(
            TestFakes.BuildDeleteEntryRequest(id: existing.Id),
            CancellationToken.None);

        // Assert
        result.Should().BeSameAs(existing);
        await entryRepo.Received(1).DeleteAsync(existing, Arg.Any<CancellationToken>());
    }

    private static ConfigEntryService CreateService(
        IConfigEntryRepository entryRepo,
        IApplicationsRepository serviceRepo,
        IDistributedLockProvider? lockFactory = null)
    {
        var factory = lockFactory ?? Substitute.For<IDistributedLockProvider>();

        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLock
            .AcquireAsync(Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(new Disposable());
        distributedLock
            .TryAcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new Disposable());

        factory
            .CreateLock(Arg.Any<string>())
            .Returns(distributedLock);

        return new ConfigEntryService(
            configEntryRepository: entryRepo,
            dateTimeProvider: new FixedDateTimeProvider(),
            logger: NullLogger<ConfigEntryService>.Instance,
            distributedLockFactory: factory,
            options: Options.Create(new ConfigEntryServiceOptions { LockTimeout = TimeSpan.FromSeconds(5) }),
            applicationsRepository: serviceRepo);
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
