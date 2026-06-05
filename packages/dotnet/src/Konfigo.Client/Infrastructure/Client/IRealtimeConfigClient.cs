using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Client.Grpc;

namespace Konfigo.Client.Infrastructure.Client;

internal interface IRealtimeConfigClient
{
    Task<IsVersionExistResponse> IsVersionExistsAsync(IsVersionExistRequest request, CancellationToken cancellationToken);
    Task<CreateVersionResponse> CreateVersionAsync(CreateVersionRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<SubscriptionEvent> StartSubscribeAsync(StartSubscribeRequest request, CancellationToken cancellationToken);
}
