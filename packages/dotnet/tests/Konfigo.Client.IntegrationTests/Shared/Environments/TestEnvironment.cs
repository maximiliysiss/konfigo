using System;
using Konfigo.Client.Options;

namespace Konfigo.Client.IntegrationTests.Shared.Environments;

internal static class TestEnvironment
{
    public static void Init()
    {
        Environment.SetEnvironmentVariable($"{nameof(RealtimeConfigOptions)}__{nameof(RealtimeConfigOptions.IsEnabled)}", "true");
        Environment.SetEnvironmentVariable($"{nameof(RealtimeConfigOptions)}__{nameof(RealtimeConfigOptions.Version)}", "1.0.0");

        var serviceId = Guid.NewGuid().ToString();
        Environment.SetEnvironmentVariable($"{nameof(RealtimeConfigOptions)}__{nameof(RealtimeConfigOptions.ServiceId)}", serviceId);

        const string urlKey = $"{nameof(RealtimeConfigOptions)}__{nameof(RealtimeConfigOptions.Url)}";
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(urlKey)))
        {
            Environment.SetEnvironmentVariable(urlKey, "http://localhost:6001");
        }
    }
}
