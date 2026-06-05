using System;
using System.Data.Common;
using Konfigo.Application.Repositories;
using Konfigo.Infrastructure.Persistence;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Infrastructure.Persistence.Migrations.Initializer;
using Konfigo.Infrastructure.Persistence.Migrations.Services;
using Konfigo.Infrastructure.Persistence.Repositories;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Publo.Abstraction.Extensions;
using Publo.Postgres.Extensions;
using StackExchange.Redis;

namespace Konfigo.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services
            .TryAddSingleton(ConfigureDataSource);

        services
            .AddScoped<IConnectionFactory, ConnectionFactory>();

        services
            .AddPublo(opt => opt.UseNpgsql<ConnectionFactory>());

        services
            .AddDbContext<AppDbContext>((sp, opt) => ConfigureDbContext(opt, sp));

        services
            .AddScoped<IConfigEntryRepository, ConfigEntryRepository>()
            .AddScoped<IConfigVersionsRepository, ConfigVersionsRepository>()
            .AddScoped<IApplicationsRepository, ApplicationRepository>()
            .AddScoped<IAuditLogRepository, AuditLogsRepository>();

        services
            .TryAddSingleton(ConfigureLock);

        services
            .TryAddSingleton<IMigrationRunner, MigrationRunner>();

        services
            .AddOptions<DatabaseInitializerOptions>()
            .BindConfiguration(nameof(DatabaseInitializerOptions));

        services
            .TryAddSingleton<IDatabaseInitializer, DatabaseInitializer>();

        return services;

        static IDistributedLockProvider ConfigureLock(IServiceProvider serviceProvider)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var connectionString = configuration.GetRequiredConnectionString("Redis");

            var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);

            return new RedisDistributedSynchronizationProvider(connectionMultiplexer.GetDatabase());
        }

        static DbDataSource ConfigureDataSource(IServiceProvider sp)
        {
            var configuration = sp.GetRequiredService<IConfiguration>();

            var builder = new NpgsqlDataSourceBuilder(configuration.GetRequiredConnectionString("Postgres"));

            return builder.Build();
        }

        static DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder opt, IServiceProvider sp)
            => opt.UseNpgsql(sp.GetRequiredService<DbDataSource>());
    }
}
