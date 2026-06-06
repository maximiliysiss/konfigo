using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Konfigo.Client.Configuration;
using Konfigo.Client.Extensions;
using Konfigo.Client.Grpc;
using Konfigo.Client.Infrastructure.Assemblies;
using Konfigo.Client.Infrastructure.Client;
using Konfigo.Client.Infrastructure.Extensions;
using Konfigo.Client.Infrastructure.HostedServices;
using Konfigo.Client.Infrastructure.Versions;
using Konfigo.Client.Models;
using Konfigo.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Konfigo.Client.Hosting;

internal sealed class RealtimeConfigHostedService : RestartableService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RealtimeConfigHostedService> _logger;

    private readonly IConfigurationRoot? _configuration;

    private readonly IAssemblyService _assemblyService;

    public RealtimeConfigHostedService(
        ILogger<RealtimeConfigHostedService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IAssemblyService assemblyService) : base(logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _assemblyService = assemblyService;
        _configuration = configuration as IConfigurationRoot;
    }

    protected override async Task ExecuteAsync(CancellationTokenSource reloadTokenSource)
    {
        var cancellationToken = reloadTokenSource.Token;

        using var scope = _serviceProvider.CreateScope();

        var options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<RealtimeConfigOptions>>();

        if (!options.Value.IsEnabled)
        {
            _logger.RealtimeConfigDisabled();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        if (_configuration is null)
        {
            _logger.ConfigurationNotAvailable();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        if (_assemblyService.GetDefinitions() is [])
        {
            _logger.NoAssembliesToScan();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        var provider = _configuration.Providers.FirstOrDefault(c => c is RealtimeConfigProvider) as RealtimeConfigProvider;
        if (provider is null)
        {
            _logger.RealtimeConfigProviderNotAvailable();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return;
        }

        var timestamp = options.Value.Timestamp;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _logger.RealtimeCycleConfigStarting();

                var service = scope.ServiceProvider.GetRequiredService<IRealtimeConfigClient>();

                var startSubscribeRequest = new StartSubscribeRequest
                {
                    ServiceId = options.Value.ServiceId,
                    Timestamp = timestamp.ToTimestamp(),
                    VersionId = options.Value.VersionId,
                };

                var eventsStream = service.StartSubscribeAsync(
                    request: startSubscribeRequest,
                    cancellationToken: cancellationToken);

                await foreach (var ev in eventsStream)
                {
                    var events = ev.Events
                        .Where(c => c.Generation > 1)
                        .ToArray();

                    if (events is [])
                        continue;

                    _logger.ReceivedEventsBatch();

                    var updates = new List<ConfigEntry>(events.Length);

                    var localTimestamp = timestamp;

                    foreach (var @event in events)
                    {
                        _logger.ReceivedEvent(@event.Key);

                        updates.Add(Map(@event));
                        localTimestamp = DateTimeOffsets.Max(localTimestamp, @event.Timestamp.ToDateTimeOffset());
                    }

                    provider.Set(updates);

                    timestamp = localTimestamp;

                    _logger.RealtimeConfigCycleFinished(updates.Count);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Graceful shutdown
                _logger.RealtimeConfigCycleCancelled();
                return;
            }
            catch (Exception ex)
            {
                _logger.RealtimeConfigCycleError(ex);
                await Task.Delay(options.Value.PollingInterval, cancellationToken);
            }
        }

        _logger.RealtimeCycleConfigFinished();

        return;

        static ConfigEntry Map(SubscriptionEvent.Types.ConfigEvent ev)
        {
            return new ConfigEntry(
                Key: ev.Key,
                Value: ev.Value,
                Generation: ev.Generation,
                Timestamp: ev.Timestamp.ToDateTimeOffset());
        }
    }
}
