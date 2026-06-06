using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Konfigo.Client.Entities;
using Konfigo.Client.Grpc;
using Konfigo.Client.Infrastructure.Assemblies;
using Konfigo.Client.Infrastructure.Client;
using Konfigo.Client.Infrastructure.Versions;
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
        var assemblyService = new AssemblyService();
        if (assemblyService.GetDefinitions() is [])
            return new RealtimeConfigProvider(VersionId.Empty, [], NullLogger<RealtimeConfigProvider>.Instance);

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

        if (response.Entries is [])
        {
            var versionService = new VersionService(
                rtcOptions: Microsoft.Extensions.Options.Options.Create(options),
                service: new RealtimeConfigClient(service),
                logger: NullLogger<VersionService>.Instance,
                assemblyService: assemblyService);

            await versionService.CreateAsync(cancellationTokenSource.Token);

            response = await service.GetConfigAsync(
                request: request,
                cancellationToken: cancellationTokenSource.Token);
        }

        return new RealtimeConfigProvider(
            versionId: new VersionId(response.VersionId),
            entries: response.Entries.Select(Map).ToArray(),
            logger: NullLogger<RealtimeConfigProvider>.Instance);

        static ConfigEntry Map(GetConfigResponse.Types.ConfigEntry c)
        {
            return new ConfigEntry(
                Key: c.Key,
                Value: c.Value,
                Type: c.Type,
                Generation: c.Generation,
                Timestamp: c.Timestamp.ToDateTimeOffset());
        }
    }
}
