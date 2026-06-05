using System.IO;
using System.Threading;
using Konfigo.Infrastructure.Extensions;
using Konfigo.IntegrationTests.Shared.Authentication;
using Konfigo.IntegrationTests.Shared.Environments;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Konfigo.IntegrationTests.Shared.Fixtures;

public sealed class IntegrationTestFixture : WebApplicationFactory<Startup>
{
    static IntegrationTestFixture() => TestEnvironment.Init();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureServices(ConfigureServices);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();

        host.RunMigrateAsync(CancellationToken.None).Wait();

        host.Start();

        return host;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        services.PostConfigureAll<AuthenticationOptions>(options =>
        {
            options.DefaultScheme = TestAuthHandler.SchemeName;
            options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            options.DefaultSignInScheme = TestAuthHandler.SchemeName;
            options.DefaultSignOutScheme = TestAuthHandler.SchemeName;
        });
    }
}
