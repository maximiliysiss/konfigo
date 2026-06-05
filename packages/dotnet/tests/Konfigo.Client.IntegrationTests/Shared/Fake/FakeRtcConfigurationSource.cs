using Konfigo.Client.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Konfigo.Client.IntegrationTests.Shared.Fake;

internal sealed class FakeRtcConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new RealtimeConfigProvider([], NullLogger<RealtimeConfigProvider>.Instance);
}
