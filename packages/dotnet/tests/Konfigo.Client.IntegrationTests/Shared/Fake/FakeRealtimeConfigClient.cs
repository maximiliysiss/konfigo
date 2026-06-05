using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Konfigo.Client.Grpc;
using Konfigo.Client.Infrastructure.Client;

namespace Konfigo.Client.IntegrationTests.Shared.Fake;

internal sealed class FakeRealtimeConfigClient : IRealtimeConfigClient
{
    private readonly Channel<SubscriptionEvent> _channel = Channel.CreateUnbounded<SubscriptionEvent>();

    public ValueTask PublishAsync(SubscriptionEvent.Types.ConfigEvent ev)
        => _channel.Writer.WriteAsync(new SubscriptionEvent { Events = { ev } });

    public Task<IsVersionExistResponse> IsVersionExistsAsync(IsVersionExistRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new IsVersionExistResponse { VersionId = Guid.NewGuid().ToString() });

    public Task<CreateVersionResponse> CreateVersionAsync(CreateVersionRequest request, CancellationToken cancellationToken)
        => throw new System.NotImplementedException();

    public IAsyncEnumerable<SubscriptionEvent> StartSubscribeAsync(StartSubscribeRequest request, CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
