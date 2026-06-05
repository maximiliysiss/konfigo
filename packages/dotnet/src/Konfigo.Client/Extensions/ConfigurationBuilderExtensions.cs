using Konfigo.Client.Configuration;
using Konfigo.Client.Options;
using Microsoft.Extensions.Configuration;

namespace Konfigo.Client.Extensions;

/// <summary>
/// <see cref="IConfigurationBuilder"/> extensions for plugging the Realtime config provider
/// into the host configuration pipeline.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds the Realtime config source to the configuration pipeline. The source fetches
    /// the initial snapshot over gRPC during host build and exposes it through
    /// <see cref="IConfiguration"/>. Live updates are applied later by the hosted service
    /// registered via <see cref="ServiceCollectionExtensions.AddRealtimeConfig"/>.
    /// </summary>
    /// <remarks>
    /// Reads <c>RealtimeConfigOptions</c> from the already-built configuration. If the section
    /// is missing or <c>IsEnabled</c> is <c>false</c>, the source is not added and the builder
    /// is returned unchanged.
    /// </remarks>
    /// <param name="builder">The configuration builder to extend.</param>
    /// <returns>The same builder, to allow call chaining.</returns>
    public static IConfigurationBuilder AddRealtimeConfig(this IConfigurationBuilder builder)
    {
        var options = builder.Build()
            .GetSection(nameof(RealtimeConfigOptions))
            .Get<RealtimeConfigOptions>();

        if (options?.IsEnabled is null or false)
        {
            return builder;
        }

        var realtimeConfigSource = new RealtimeConfigSource(options);

        builder.Add(realtimeConfigSource);

        return builder;
    }
}
