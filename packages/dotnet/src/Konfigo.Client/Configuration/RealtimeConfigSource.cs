using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Konfigo.Client.Grpc;
using Konfigo.Client.Models;
using Konfigo.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Konfigo.Client.Configuration;

internal sealed class RealtimeConfigSource(RealtimeConfigOptions options) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => BuildAsync().GetAwaiter().GetResult();

    private async Task<IConfigurationProvider> BuildAsync()
    {
        using var cancellationTokenSource = new CancellationTokenSource(delay: options.InitialRequestDelay);

        using var channel = GrpcChannel.ForAddress(options.Url);

        var service = new RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient(channel);

        var request = new GetConfigRequest
        {
            ServiceId = options.ServiceId,
            Version = options.Version,
        };

        var response = await service.GetConfigAsync(
            request: request,
            cancellationToken: cancellationTokenSource.Token);

        return new RealtimeConfigProvider(
            entries: response.Entries.Select(Map).ToArray(),
            logger: NullLogger<RealtimeConfigProvider>.Instance);

        static ConfigEntry Map(GetConfigResponse.Types.ConfigEntry c)
        {
            return new ConfigEntry(
                Key: c.Key,
                Value: c.Value,
                Generation: c.Generation,
                Timestamp: c.Timestamp.ToDateTimeOffset());
        }
    }
}
