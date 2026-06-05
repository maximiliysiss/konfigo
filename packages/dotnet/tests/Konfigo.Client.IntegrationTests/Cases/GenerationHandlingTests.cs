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
public class GenerationHandlingTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Handler_ShouldIgnoreInitialGenerationEvents()
    {
        // Arrange
        var service = fixture.Services.GetRequiredService<FakeRealtimeConfigClient>();

        // Act
        await service.PublishAsync(new SubscriptionEvent.Types.ConfigEvent
        {
            Generation = 1,
            Key = $"{nameof(InitialGenerationOptions)}:{nameof(InitialGenerationOptions.Value)}",
            Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
            Value = "44",
        });

        await Task.Delay(TimeSpan.FromSeconds(1));

        var options = fixture.Services.GetRequiredService<IOptionsMonitor<InitialGenerationOptions>>();

        // Assert
        options.CurrentValue.Value.Should().Be(42);
    }

    [Fact]
    public async Task Handler_ShouldIgnoreStaleGenerationEvents()
    {
        // Arrange
        var service = fixture.Services.GetRequiredService<FakeRealtimeConfigClient>();

        // Act
        await service.PublishAsync(new SubscriptionEvent.Types.ConfigEvent
        {
            Generation = 42,
            Key = $"{nameof(StaleGenerationOptions)}:{nameof(StaleGenerationOptions.Value)}",
            Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
            Value = "44",
        });

        await Task.Delay(TimeSpan.FromSeconds(1));

        await service.PublishAsync(new SubscriptionEvent.Types.ConfigEvent
        {
            Generation = 41,
            Key = $"{nameof(StaleGenerationOptions)}:{nameof(StaleGenerationOptions.Value)}",
            Timestamp = DateTimeOffset.UtcNow.ToTimestamp(),
            Value = "45",
        });

        await Task.Delay(TimeSpan.FromSeconds(1));

        var options = fixture.Services.GetRequiredService<IOptionsMonitor<StaleGenerationOptions>>();

        // Assert
        options.CurrentValue.Value.Should().Be(44);
    }
}
