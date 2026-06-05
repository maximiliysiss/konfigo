using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Konfigo.Client.Grpc;

namespace Konfigo.Client.Infrastructure.Client;

internal sealed class RealtimeConfigClient(RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient client) : IRealtimeConfigClient
{
    public async Task<IsVersionExistResponse> IsVersionExistsAsync(IsVersionExistRequest request, CancellationToken cancellationToken)
        => await client.IsVersionExistsAsync(request, cancellationToken: cancellationToken);

    public async Task<CreateVersionResponse> CreateVersionAsync(CreateVersionRequest request, CancellationToken cancellationToken)
        => await client.CreateVersionAsync(request, cancellationToken: cancellationToken);

    public IAsyncEnumerable<SubscriptionEvent> StartSubscribeAsync(StartSubscribeRequest request, CancellationToken cancellationToken)
    {
        return client
            .StartSubscribe(request, cancellationToken: cancellationToken)
            .ResponseStream
            .ReadAllAsync(cancellationToken);
    }
}
