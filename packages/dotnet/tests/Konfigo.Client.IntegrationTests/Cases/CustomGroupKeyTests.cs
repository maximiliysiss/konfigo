using System;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Konfigo.Client.Grpc;
using Konfigo.Client.IntegrationTests.Shared.Fake;
using Konfigo.Client.IntegrationTests.Shared.Fixtures;
using Konfigo.Client.IntegrationTests.Shared.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Konfigo.Client.IntegrationTests.Cases;

[Collection(nameof(IntegrationTestCollection))]
public class CustomGroupKeyTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldBindOptionsByConfigGroupKey()
    {
        // Arrange
        var service = fixture.Services.GetRequiredService<FakeRealtimeConfigClient>();

        // Act
        await service.PublishAsync(new SubscriptionEvent.Types.ConfigEvent
        {
            Generation = 42,
            Key = $"{CustomKeyOptions.SectionName}:{nameof(CustomKeyOptions.Value)}",
            Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
            Value = "9",
        });

        await Task.Delay(TimeSpan.FromSeconds(1));

        var options = fixture.Services.GetRequiredService<IOptionsMonitor<CustomKeyOptions>>();

        // Assert
        options.CurrentValue.Value.Should().Be(9);
    }
}
