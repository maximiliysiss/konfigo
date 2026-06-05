using System.Collections.Generic;
using FluentAssertions;
using Konfigo.Client.Extensions;
using Konfigo.Client.UnitTests.Shared.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Konfigo.Client.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRtcOptions_ShouldBindDiscoveredConfigGroups()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(CommonOptions)}:{nameof(CommonOptions.IntValue)}"] = "42",
            })
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();

        // Act
        services.AddRealtimeConfig();

        // Assert
        using var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<IOptions<CommonOptions>>();

        options.Value.IntValue.Should().Be(42);
    }
}
