using System;
using Microsoft.Extensions.Configuration;

namespace Konfigo.Infrastructure.Extensions;

internal static class ConfigurationExtensiosn
{
    public static string GetRequiredConnectionString(this IConfiguration configuration, string name)
        => configuration.GetConnectionString(name) ?? throw new InvalidOperationException($"Connection string '{name}' is not configured");
}
