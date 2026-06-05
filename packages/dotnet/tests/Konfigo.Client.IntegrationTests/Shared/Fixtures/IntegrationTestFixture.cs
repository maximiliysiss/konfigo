using System.IO;
using Konfigo.Client.Extensions;
using Konfigo.Client.Infrastructure.Client;
using Konfigo.Client.IntegrationTests.Shared.Environments;
using Konfigo.Client.IntegrationTests.Shared.Fake;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Konfigo.Client.IntegrationTests.Shared.Fixtures;

public class IntegrationTestFixture : WebApplicationFactory<IntegrationTestFixture.Startup>
{
    static IntegrationTestFixture() => TestEnvironment.Init();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder
            .UseStartup<Startup>()
            .UseContentRoot(Directory.GetCurrentDirectory());
    }

    protected override IHostBuilder CreateHostBuilder() => Host
        .CreateDefaultBuilder()
        .ConfigureServices(ConfigureServices)
        .ConfigureAppConfiguration(b => b.Add(new FakeRtcConfigurationSource()));

    private void ConfigureServices(IServiceCollection services)
    {
        var fakeRealtimeStream = new FakeRealtimeConfigClient();

        services
            .AddSingleton(fakeRealtimeStream);

        services
            .AddRealtimeConfig();

        services
            .AddSingleton<FakeRealtimeConfigClient>()
            .AddSingleton<IRealtimeConfigClient>(sp => sp.GetRequiredService<FakeRealtimeConfigClient>());
    }

    public sealed class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
        }

        public static void Configure()
        {
        }
    }
}
