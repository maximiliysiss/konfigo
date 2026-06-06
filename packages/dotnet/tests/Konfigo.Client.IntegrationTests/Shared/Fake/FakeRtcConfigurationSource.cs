using Konfigo.Client.Configuration;
using Konfigo.Client.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Konfigo.Client.IntegrationTests.Shared.Fake;

internal sealed class FakeRtcConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new RealtimeConfigProvider(VersionId.Empty, [], NullLogger<RealtimeConfigProvider>.Instance);
}
