using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Services.Configurations;
using Konfigo.Application.Services.Configurations.Models;
using Konfigo.Application.Services.Configurations.Tracking;
using Konfigo.Application.Services.Notifications;
using Konfigo.Application.Services.Notifications.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.UnitTests.Support;
using NSubstitute;
using Xunit;
using UpdateEntryRequest = Konfigo.Application.Services.Configurations.Models.UpdateEntryRequest;

namespace Konfigo.UnitTests.Application.Configurations;

public sealed class TrackingConfigEntryServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldNotifyAllNotifiers_WhenCalled()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var notifierA = Substitute.For<IConfigChangeNotifier>();
        var notifierB = Substitute.For<IConfigChangeNotifier>();

        var request = TestFakes.BuildCreateEntryRequest();
        var created = TestFakes.BuildEntry(versionId: request.VersionId);
        inner.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(created);

        var sut = new TrackingConfigEntryService(inner, [notifierA, notifierB]);

        // Act
        await sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await notifierA.Received(1).HandleAsync(
            Arg.Is<NotificationRequest>(n =>
                n.ServiceId == request.ServiceId &&
                n.VersionId == request.VersionId &&
                n.Requests.Length == 1 &&
                n.Requests[0].EntryId == created.Id),
            Arg.Any<CancellationToken>());
        await notifierB.Received(1).HandleAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotifyAllNotifiers_WhenUnderlyingReturnsEntry()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var notifier = Substitute.For<IConfigChangeNotifier>();

        var request = TestFakes.BuildUpdateEntryRequest();
        var updated = TestFakes.BuildEntry(versionId: request.VersionId);
        inner.UpdateAsync(request, Arg.Any<CancellationToken>()).Returns(updated);

        var sut = new TrackingConfigEntryService(inner, [notifier]);

        // Act
        await sut.UpdateAsync(request, CancellationToken.None);

        // Assert
        await notifier.Received(1).HandleAsync(
            Arg.Is<NotificationRequest>(n =>
                n.Requests.Length == 1 && n.Requests[0].EntryId == updated.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotNotify_WhenUnderlyingReturnsNull()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var notifier = Substitute.For<IConfigChangeNotifier>();
        inner.UpdateAsync(Arg.Any<UpdateEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns((ConfigEntry?)null);

        var sut = new TrackingConfigEntryService(inner, [notifier]);

        // Act
        var result = await sut.UpdateAsync(TestFakes.BuildUpdateEntryRequest(), CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await notifier.DidNotReceive().HandleAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_ShouldNotNotify_WhenResultIsEmpty()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var notifier = Substitute.For<IConfigChangeNotifier>();
        inner.SetAsync(Arg.Any<SetEntryRequest>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = new TrackingConfigEntryService(inner, [notifier]);

        var request = new SetEntryRequest(
            ServiceId.New(),
            VersionId.New(),
            [],
            new UserDto(new UserId(Guid.NewGuid().ToString()), Roles: []));

        // Act
        var result = await sut.SetAsync(request, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        await notifier.DidNotReceive().HandleAsync(Arg.Any<NotificationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAsync_ShouldNotifyAllNotifiers_WhenResultHasEntries()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var notifier = Substitute.For<IConfigChangeNotifier>();

        var serviceId = ServiceId.New();
        var versionId = VersionId.New();
        var request = new SetEntryRequest(
            serviceId,
            versionId,
            [new SetEntryRequest.SetRequest(EntryId.New(), "v", 1)],
            new UserDto(new UserId(Guid.NewGuid().ToString()), Roles: ["billing"]));

        var returned = new[]
        {
            TestFakes.BuildEntry(versionId: versionId),
            TestFakes.BuildEntry(versionId: versionId),
        };
        inner.SetAsync(request, Arg.Any<CancellationToken>()).Returns(returned);

        var sut = new TrackingConfigEntryService(inner, [notifier]);

        // Act
        await sut.SetAsync(request, CancellationToken.None);

        // Assert
        await notifier.Received(1).HandleAsync(
            Arg.Is<NotificationRequest>(n =>
                n.ServiceId == serviceId &&
                n.VersionId == versionId &&
                n.Requests.Length == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotifyAllNotifiers_WhenUnderlyingReturnsEntry()
    {
        // Arrange
        var inner = Substitute.For<IConfigEntryService>();
        var notifier = Substitute.For<IConfigChangeNotifier>();

        var request = TestFakes.BuildDeleteEntryRequest();
        var deleted = TestFakes.BuildEntry(id: request.Id, versionId: request.VersionId);
        inner.DeleteAsync(request, Arg.Any<CancellationToken>()).Returns(deleted);

        var sut = new TrackingConfigEntryService(inner, [notifier]);

        // Act
        await sut.DeleteAsync(request, CancellationToken.None);

        // Assert
        await notifier.Received(1).HandleAsync(
            Arg.Is<NotificationRequest>(n =>
                n.Requests.Single().EntryId == deleted.Id),
            Arg.Any<CancellationToken>());
    }
}
