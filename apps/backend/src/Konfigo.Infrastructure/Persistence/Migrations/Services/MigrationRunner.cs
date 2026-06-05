using System;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.VersionTableInfo;
using Konfigo.Infrastructure.Persistence.Migrations.Configuration;
using Konfigo.Infrastructure.Persistence.Migrations.Infrastructure;
using Konfigo.Infrastructure.Persistence.Migrations.Initializer;
using Medallion.Threading.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Publo.Postgres.Infrastructure.Database;

namespace Konfigo.Infrastructure.Persistence.Migrations.Services;

internal sealed class MigrationRunner : IMigrationRunner
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(10);

    private readonly IConnectionFactory _connectionFactory;

    private readonly ILoggerFactory _loggerFactory;

    private readonly IDatabaseInitializer _databaseInitializer;

    public MigrationRunner(IConnectionFactory connectionFactory, ILoggerFactory loggerFactory, IDatabaseInitializer databaseInitializer)
    {
        _connectionFactory = connectionFactory;
        _loggerFactory = loggerFactory;
        _databaseInitializer = databaseInitializer;
    }

    public async Task MigrateUpAsync(CancellationToken cancellationToken)
    {
        await _databaseInitializer.InitializeAsync(cancellationToken);

        var @lock = new PostgresDistributedLock(
            key: new PostgresAdvisoryLockKey(nameof(MigrationRunner), allowHashing: true),
            connectionString: _connectionFactory.GetConnectionString());

        await using var _ = await @lock.AcquireAsync(_defaultTimeout, cancellationToken: cancellationToken);

        // Create scope
        await using var serviceProvider = new ServiceCollection()
            .AddScoped<IVersionTableMetaData, VersionTableMetaData>()
            .AddScoped<IVersionTableMetaDataAccessor, VersionTableMetaDataAccessor>()
            .AddSingleton<IMigrationSourceItem, MigrationSourceItem>()
            .AddFluentMigratorCore()
            .ConfigureRunner(builder => builder.AddPostgres())
            .AddOptions<ProcessorOptions>()
            .Configure(ConfigureOptions)
            .Services
            .AddSingleton(_loggerFactory)
            .BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();

        // Migrate
        var runner = serviceProvider.GetRequiredService<FluentMigrator.Runner.IMigrationRunner>();

        runner.MigrateUp();

        // Reload types
        await using var connection = await _connectionFactory.GetConnectionAsync(cancellationToken);

        await using var npgsqlConnection = connection as NpgsqlConnection;

        if (npgsqlConnection is not null)
        {
            await npgsqlConnection.OpenAsync(cancellationToken);
            await npgsqlConnection.ReloadTypesAsync(cancellationToken);
        }

        return;

        void ConfigureOptions(ProcessorOptions options)
        {
            options.ProviderSwitches = "Force Quote=false";
            options.Timeout = _defaultTimeout;
            options.ConnectionString = _connectionFactory.GetConnectionString();
        }
    }
}
