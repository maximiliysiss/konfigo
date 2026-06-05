using System.Threading;
using System.Threading.Tasks;
using Konfigo.Infrastructure.Persistence.Factory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Konfigo.Infrastructure.Persistence.Migrations.Initializer;

internal sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IConnectionFactory _connectionFactory;

    private readonly DatabaseInitializerOptions _options;

    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IConnectionFactory connectionFactory,
        IOptions<DatabaseInitializerOptions> options,
        ILogger<DatabaseInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing database: {DatabaseName}", _options.DatabaseName);

        var connectionString = new NpgsqlConnectionStringBuilder(_connectionFactory.GetConnectionString()) { Database = "postgres" };

        await using var connection = new NpgsqlConnection(connectionString.ConnectionString);

        await using var command = connection.CreateCommand();

        command.CommandText = $"CREATE DATABASE {_options.DatabaseName};";

        await connection.OpenAsync(cancellationToken);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P04")
        {
            _logger.LogInformation("Database already exists: {DatabaseName}", _options.DatabaseName);
        }

        _logger.LogInformation("Database initialized: {DatabaseName}", _options.DatabaseName);
    }
}
