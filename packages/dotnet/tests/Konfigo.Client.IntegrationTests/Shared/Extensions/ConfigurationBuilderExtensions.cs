using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Konfigo.Client.IntegrationTests.Shared.Extensions;

internal static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder configurationBuilder, string? basePath = null)
    {
        basePath ??= string.Empty;

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local";

        return configurationBuilder
            .AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(basePath, $"appsettings.{environment}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
    }
}
