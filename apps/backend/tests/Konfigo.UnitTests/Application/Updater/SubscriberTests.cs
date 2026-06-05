using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Konfigo.Application.Services.Updater.Models;
using Konfigo.Domain.ValueType;
using Xunit;

namespace Konfigo.UnitTests.Application.Updater;

public sealed class SubscriberTests
{
    [Fact]
    public async Task PublishAsync_ShouldYieldEventToSubscriber_WhenSubscribeAsyncIterated()
    {
        // Arrange
        var subscriber = new Subscriber(onDispose: _ => ValueTask.CompletedTask);
        var changeEvent = new ChangeEvent(ServiceId.New(), VersionId.New(), []);

        // Act
        await subscriber.PublishAsync(changeEvent, CancellationToken.None);

        // Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var enumerator = subscriber.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var hasNext = await enumerator.MoveNextAsync();
        hasNext.Should().BeTrue();
        enumerator.Current.Should().BeSameAs(changeEvent);
    }

    [Fact]
    public async Task DisposeAsync_ShouldInvokeOnDisposeCallback_WhenCalled()
    {
        // Arrange
        Subscriber? captured = null;
        var subscriber = new Subscriber(onDispose: s =>
        {
            captured = s;
            return ValueTask.CompletedTask;
        });

        // Act
        await subscriber.DisposeAsync();

        // Assert
        captured.Should().BeSameAs(subscriber);
    }
}
