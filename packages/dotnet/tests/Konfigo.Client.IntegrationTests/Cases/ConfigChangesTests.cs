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
public class ConfigChangesTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldApplyChanged()
    {
        // Arrange
        var delay = TimeSpan.FromSeconds(2);

        var service = fixture.Services.GetRequiredService<FakeRealtimeConfigClient>();

        _ = Task.Run(Cycle);

        // Act
        ChangedOptions firstOptions;
        ChangedOptions secondOptions;

        using (var scope = fixture.Services.CreateScope())
        {
            firstOptions = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ChangedOptions>>().Value;
        }

        await Task.Delay(delay * 1.5);

        using (var scope = fixture.Services.CreateScope())
        {
            secondOptions = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ChangedOptions>>().Value;
        }

        // Assert
        firstOptions.Value.Should().Be(42);
        secondOptions.Value.Should().Be(44);

        return;

        async Task Cycle()
        {
            await Task.Delay(delay);

            var configEvent = new SubscriptionEvent.Types.ConfigEvent()
            {
                Generation = 42,
                Key = $"{nameof(ChangedOptions)}:{nameof(ChangedOptions.Value)}",
                Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
                Value = "44",
            };

            await service.PublishAsync(configEvent);
        }
    }
}
