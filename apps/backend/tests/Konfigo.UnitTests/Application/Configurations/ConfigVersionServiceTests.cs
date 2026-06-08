using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Options;
using Konfigo.Domain.Entities;
using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using Medallion.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Konfigo.UnitTests.Application.Configurations;

public sealed class ConfigVersionServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistVersion_WhenCalled()
    {
        // Arrange
        var repo = Substitute.For<IConfigVersionsRepository>();
        repo.AddAsync(Arg.Any<ConfigVersion>(), Arg.Any<CancellationToken>())
            .Returns(c => c.Arg<ConfigVersion>());
        var sut = CreateService(repo);
        var request = TestFakes.BuildCreateVersionRequest(versionLabel: "v9", description: "d9");

        // Act
        var result = await sut.CreateAsync(request, CancellationToken.None);

        // Assert
        result.VersionLabel.Should().Be("v9");
        result.Description.Should().Be("d9");
        result.ServiceId.Should().Be(request.ServiceId);
        result.CreatedAt.Should().Be(TestFakes.Now);
        await repo.Received(1).AddAsync(result, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenVersionNotFound()
    {
        // Arrange
        var repo = Substitute.For<IConfigVersionsRepository>();
        repo.GetAsync(Arg.Any<SearchVersionRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.Empty<ConfigVersion>());

        var sut = CreateService(repo);

        // Act
        var result = await sut.UpdateAsync(TestFakes.BuildUpdateVersionRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await repo.DidNotReceive().UpdateAsync(Arg.Any<ConfigVersion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldMutateAndPersist_WhenFound()
    {
        // Arrange
        var repo = Substitute.For<IConfigVersionsRepository>();
        var existing = TestFakes.BuildVersion(versionLabel: "v1", description: "old");
        repo.GetAsync(Arg.Any<SearchVersionRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existing));

        var sut = CreateService(repo);
        var request = TestFakes.BuildUpdateVersionRequest(
            serviceId: existing.ServiceId,
            versionId: existing.Id,
            versionLabel: "v2",
            description: "new");

        // Act
        var result = await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(existing);
        existing.VersionLabel.Should().Be("v2");
        existing.Description.Should().Be("new");
        await repo.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ShouldReuseExistingRawValue_WhenKeyAndValueTypeMatch()
    {
        // Arrange
        var repo = Substitute.For<IConfigVersionsRepository>();
        var serviceId = ServiceId.New();

        var existingVersion = TestFakes.BuildVersion(serviceId: serviceId);
        var existingEntry = TestFakes.BuildEntry(
            key: "shared",
            rawValue: "keep-me",
            valueType: ConfigValueType.String);
        existingVersion.ConfigEntries = [existingEntry];

        repo.GetAsync(Arg.Any<SearchVersionRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                AsyncEnumerableHelper.Empty<ConfigVersion>(),
                AsyncEnumerableHelper.From(existingVersion));
        repo.AddAsync(Arg.Any<ConfigVersion>(), Arg.Any<CancellationToken>())
            .Returns(c => c.Arg<ConfigVersion>());

        var sut = CreateService(repo);
        var request = TestFakes.BuildGenerateVersionRequest(
            serviceId: serviceId,
            versionLabel: "v2",
            entries: TestFakes.BuildGenerateEntryRequest(
                key: "shared",
                rawValue: "ignored-because-of-existing",
                valueType: ConfigValueType.String));

        // Act
        var result = await sut.GenerateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Konfigo.Application.Services.Configurations.Models.GenerateResult.New>();
        var single = result.Version.ConfigEntries.Single();
        single.RawValue.Should().Be("keep-me");
        single.Key.Should().Be("shared");
        single.ConfigVersionId.Should().Be(result.Version.Id);
    }

    [Fact]
    public async Task GenerateAsync_ShouldUseRequestRawValue_WhenValueTypeDiffersOrKeyMissing()
    {
        // Arrange
        var repo = Substitute.For<IConfigVersionsRepository>();
        var serviceId = ServiceId.New();

        var existingVersion = TestFakes.BuildVersion(serviceId: serviceId);
        var existingEntry = TestFakes.BuildEntry(
            key: "shared",
            rawValue: "keep-me",
            valueType: ConfigValueType.Number);
        existingVersion.ConfigEntries = [existingEntry];

        repo.GetAsync(Arg.Any<SearchVersionRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                AsyncEnumerableHelper.Empty<ConfigVersion>(),
                AsyncEnumerableHelper.From(existingVersion));
        repo.AddAsync(Arg.Any<ConfigVersion>(), Arg.Any<CancellationToken>())
            .Returns(c => c.Arg<ConfigVersion>());

        var sut = CreateService(repo);
        var request = TestFakes.BuildGenerateVersionRequest(
            serviceId: serviceId,
            entries:
            [
                TestFakes.BuildGenerateEntryRequest(
                    key: "shared",
                    rawValue: "type-mismatch-uses-new",
                    valueType: ConfigValueType.String),
                TestFakes.BuildGenerateEntryRequest(
                    key: "brand-new",
                    rawValue: "new-key-uses-new",
                    valueType: ConfigValueType.String),
            ]);

        // Act
        var result = await sut.GenerateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Konfigo.Application.Services.Configurations.Models.GenerateResult.New>();
        result.Version.ConfigEntries.Should().HaveCount(2);
        result.Version.ConfigEntries.Single(c => c.Key == "shared").RawValue.Should().Be("type-mismatch-uses-new");
        result.Version.ConfigEntries.Single(c => c.Key == "brand-new").RawValue.Should().Be("new-key-uses-new");
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnExistingVersionWithoutAdding_WhenVersionLabelAlreadyExists()
    {
        // Arrange
        var repo = Substitute.For<IConfigVersionsRepository>();
        var existingVersion = TestFakes.BuildVersion(versionLabel: "v2");
        existingVersion.ConfigEntries =
        [
            TestFakes.BuildEntry(versionId: existingVersion.Id),
        ];

        repo.GetAsync(Arg.Any<SearchVersionRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableHelper.From(existingVersion));

        var sut = CreateService(repo);
        var request = TestFakes.BuildGenerateVersionRequest(
            serviceId: existingVersion.ServiceId,
            versionLabel: "v2",
            entries: TestFakes.BuildGenerateEntryRequest());

        // Act
        var result = await sut.GenerateAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Konfigo.Application.Services.Configurations.Models.GenerateResult.Exists>();
        result.Version.Should().BeSameAs(existingVersion);
        await repo.DidNotReceive().AddAsync(Arg.Any<ConfigVersion>(), Arg.Any<CancellationToken>());
    }

    private static ConfigVersionService CreateService(IConfigVersionsRepository repo)
    {
        var factory = Substitute.For<IDistributedLockProvider>();

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

        return new ConfigVersionService(
            repository: repo,
            dateTimeProvider: new FixedDateTimeProvider(),
            logger: NullLogger<ConfigVersionService>.Instance,
            distributedLockProvider: factory,
            options: Options.Create(new ConfigVersionServiceOptions()));
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
