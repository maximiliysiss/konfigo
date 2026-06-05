using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Services.Updater;
using Konfigo.Application.Services.Updater.Models;
using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Konfigo.UnitTests.Application.Updater;

public sealed class UpdaterServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldReturnSubscriber_WhenCalled()
    {
        // Arrange
        var sut = new UpdaterService(NullLogger<UpdaterService>.Instance);
        var request = new CreateSubscriberRequest(ServiceId.New(), VersionId.New());

        // Act
        var subscriber = await sut.CreateAsync(request, CancellationToken.None);

        // Assert
        subscriber.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_ShouldDeliverEventToMatchingSubscribers_WhenCalled()
    {
        // Arrange
        var sut = new UpdaterService(NullLogger<UpdaterService>.Instance);
        var serviceId = ServiceId.New();
        var versionId = VersionId.New();
        var subscriber = await sut.CreateAsync(
            new CreateSubscriberRequest(serviceId, versionId),
            CancellationToken.None);
        var changeEvent = new ChangeEvent(serviceId, versionId, []);

        // Act
        await sut.PublishAsync(changeEvent, CancellationToken.None);

        // Assert
        var received = await ReadOneAsync(subscriber);
        received.Should().BeSameAs(changeEvent);
    }

    [Fact]
    public async Task PublishAsync_ShouldBeNoOp_WhenNoSubscribersForKey()
    {
        // Arrange
        var sut = new UpdaterService(NullLogger<UpdaterService>.Instance);
        var changeEvent = new ChangeEvent(ServiceId.New(), VersionId.New(), []);

        // Act
        var act = async () => await sut.PublishAsync(changeEvent, CancellationToken.None);

        // Assert
        await ((Func<Task>)act).Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_ShouldDeliverEventToAllSubscribers_WhenManyForSameKey()
    {
        // Arrange
        var sut = new UpdaterService(NullLogger<UpdaterService>.Instance);
        var serviceId = ServiceId.New();
        var versionId = VersionId.New();
        var first = await sut.CreateAsync(
            new CreateSubscriberRequest(serviceId, versionId),
            CancellationToken.None);
        var second = await sut.CreateAsync(
            new CreateSubscriberRequest(serviceId, versionId),
            CancellationToken.None);
        var changeEvent = new ChangeEvent(serviceId, versionId, []);

        // Act
        await sut.PublishAsync(changeEvent, CancellationToken.None);

        // Assert
        var firstReceived = await ReadOneAsync(first);
        var secondReceived = await ReadOneAsync(second);
        firstReceived.Should().BeSameAs(changeEvent);
        secondReceived.Should().BeSameAs(changeEvent);
    }

    [Fact]
    public async Task Subscriber_DisposeAsync_ShouldRemoveSubscriberFromUpdater_WhenInvoked()
    {
        // Arrange
        var sut = new UpdaterService(NullLogger<UpdaterService>.Instance);
        var serviceId = ServiceId.New();
        var versionId = VersionId.New();
        var subscriber = await sut.CreateAsync(
            new CreateSubscriberRequest(serviceId, versionId),
            CancellationToken.None);
        var beforeDispose = new ChangeEvent(serviceId, versionId, []);
        var afterDispose = new ChangeEvent(serviceId, versionId, []);
        await sut.PublishAsync(beforeDispose, CancellationToken.None);

        // Act
        await subscriber.DisposeAsync();
        await sut.PublishAsync(afterDispose, CancellationToken.None);

        // Assert
        var first = await ReadOneAsync(subscriber);
        first.Should().BeSameAs(beforeDispose);
        await AssertNoMoreAsync(subscriber);
    }

    private static async Task<ChangeEvent> ReadOneAsync(Subscriber subscriber)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var enumerator = subscriber.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var hasNext = await enumerator.MoveNextAsync();
        hasNext.Should().BeTrue();
        return enumerator.Current;
    }

    private static async Task AssertNoMoreAsync(Subscriber subscriber)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var enumerator = subscriber.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var act = async () => await enumerator.MoveNextAsync();
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
