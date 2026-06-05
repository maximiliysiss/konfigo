using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Konfigo.Application.Services.Updater.Models;

public sealed class Subscriber(Func<Subscriber, ValueTask> onDispose) : IAsyncDisposable
{
    private readonly Channel<ChangeEvent> _channel = Channel.CreateBounded<ChangeEvent>(capacity: 100);

    public IAsyncEnumerable<ChangeEvent> SubscribeAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);

    public ValueTask PublishAsync(ChangeEvent changeEvent, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(changeEvent, cancellationToken);

    public ValueTask DisposeAsync() => onDispose(this);
}
