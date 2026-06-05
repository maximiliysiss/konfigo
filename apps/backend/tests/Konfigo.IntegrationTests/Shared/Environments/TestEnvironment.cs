using System;

namespace Konfigo.IntegrationTests.Shared.Environments;

internal static class TestEnvironment
{
    public static void Init()
    {
        const string environmentKey = "ASPNETCORE_ENVIRONMENT";

        var environmentVariable = Environment.GetEnvironmentVariable(environmentKey);
        if (string.IsNullOrEmpty(environmentVariable))
            Environment.SetEnvironmentVariable(environmentKey, "Local");
    }
}
