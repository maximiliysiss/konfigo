using System.Threading;
using System.Threading.Tasks;
using Konfigo.Infrastructure.Persistence.Migrations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Konfigo.Infrastructure.Extensions;

public static class HostExtensions
{
    public static async Task<IHost> RunMigrateAsync(this IHost host, CancellationToken cancellationToken)
    {
        var logger = host.Services.GetRequiredService<ILogger<MigrationRunner>>();

        logger.LogRunningMigrations();

        using var serviceScope = host.Services.CreateScope();

        var runner = serviceScope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        await runner.MigrateUpAsync(cancellationToken);

        logger.LogMigrationsCompleted();

        return host;
    }
}
