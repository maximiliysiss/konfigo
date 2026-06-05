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
public class ReactiveConfigChangesTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldApplyChanged()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(2);

        var service = fixture.Services.GetRequiredService<FakeRealtimeConfigClient>();

        var task = Task.Run(Cycle);

        var isTriggered = false;

        // Act
        var optionsMonitor = fixture.Services.GetRequiredService<IOptionsMonitor<ReactiveChangedOptions>>();

        using var _ = optionsMonitor.OnChange((_, _) => isTriggered = true);

        await Task.Delay(delay * 1.5);

        // Assert
        isTriggered.Should().BeTrue();

        return;

        async Task Cycle()
        {
            await Task.Delay(delay);

            var configEvent = new SubscriptionEvent.Types.ConfigEvent
            {
                Generation = 42,
                Key = $"{nameof(ReactiveChangedOptions)}:{nameof(ReactiveChangedOptions.Value)}",
                Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
                Value = "44",
            };

            await service.PublishAsync(configEvent);
        }
    }
}
