using System;
using System.Net.Http;
using Grpc.Core;
using Konfigo.Client.Grpc;
using Konfigo.Client.Hosting;
using Konfigo.Client.Infrastructure.Assemblies;
using Konfigo.Client.Infrastructure.Client;
using Konfigo.Client.Infrastructure.Extensions;
using Konfigo.Client.Infrastructure.Versions;
using Konfigo.Client.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Konfigo.Client.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for registering the Realtime config runtime.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Realtime config services: options binding, version/assembly services,
    /// the hosted service that subscribes to streaming config updates, and
    /// <c>IOptions&lt;T&gt;</c> bindings for every class annotated with
    /// <see cref="Abstraction.Attributes.ConfigGroupAttribute"/> discovered in the loaded
    /// assemblies.
    /// </summary>
    /// <remarks>
    /// Pair this with <see cref="ConfigurationBuilderExtensions.AddRealtimeConfig"/> on the
    /// host configuration builder so that the configuration source is in place before the
    /// hosted service starts.
    /// </remarks>
    /// <param name="services">The service collection to extend.</param>
    /// <returns>The same service collection, to allow call chaining.</returns>
    public static IServiceCollection AddRealtimeConfig(this IServiceCollection services)
    {
        services
            .AddOptions<RealtimeConfigOptions>()
            .BindConfiguration(nameof(RealtimeConfigOptions));

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError() // Handles HttpRequestException, 5XX, 408
            .Or<RpcException>(e => e.StatusCode == StatusCode.Unavailable) // Specifically handle gRPC unavailable
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        services
            .AddGrpcClient<RealtimeConfigGrpcService.RealtimeConfigGrpcServiceClient>()
            .ConfigureHttpClient((sp, opt) => ConfigureClient(opt, sp))
            .AddPolicyHandler(retryPolicy);

        services
            .AddScoped<IRealtimeConfigClient, RealtimeConfigClient>();

        services
            .TryAddScoped<IVersionService, VersionService>();

        services
            .TryAddSingleton<IAssemblyService, AssemblyService>();

        services
            .AddHostedService<RealtimeConfigHostedService>();

        services
            .AddRtcOptions();

        return services;

        void ConfigureClient(HttpClient opt, IServiceProvider sp)
        {
            var options = sp.GetRequiredService<IOptions<RealtimeConfigOptions>>();
            opt.BaseAddress = new Uri(options.Value.Url);
        }
    }

    private static IServiceCollection AddRtcOptions(this IServiceCollection services)
    {
        var genericType = typeof(OptionsBuilder<>);

        foreach (var classDefinition in Assemblies.GetDefinitions())
        {
            var type = genericType.MakeGenericType(classDefinition.Type);

            dynamic? builder = Activator.CreateInstance(type, services, string.Empty);

            OptionsBuilderConfigurationExtensions.BindConfiguration(builder, classDefinition.Key);
        }

        return services;
    }
}
