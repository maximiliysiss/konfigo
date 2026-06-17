using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Infrastructure.Persistence.Npgsql;
using Konfigo.IntegrationTests.DbHelpers.Shared;
using Npgsql;

namespace Konfigo.IntegrationTests.DbHelpers;

internal sealed class ConfigVersionDbHelper : IDbHelper, ITracker<Guid>
{
    private readonly HashSet<Guid> _ids = [];

    private readonly IConnectionFactory _connectionFactory;

    public ConfigVersionDbHelper(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<TableRow?> GetAsync(Guid id)
    {
        const string sql = @"
SELECT id, service_id, version_label, description, created_at, updated_at
FROM public.config_versions
WHERE id = :id;
";
        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(sql, connection) { Parameters = { { "id", id }, } };

        await connection.OpenAsync();

        var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new TableRow
        {
            Id = id,
            ServiceId = reader.GetFieldValue<Guid>("service_id"),
            VersionLabel = reader.GetString("version_label"),
            Description = reader.GetNullableString("description"),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
        };
    }

    public async Task InsertAsync(TableRow row)
    {
        const string sql = @"
INSERT INTO public.config_versions
    (id, service_id, version_label, description, created_at, updated_at)
VALUES
    (:id, :serviceId, :versionLabel, :description, :createdAt, :updatedAt);
";
        _ids.Add(row.Id);

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(sql, connection)
        {
            Parameters =
            {
                { "id", row.Id },
                { "serviceId", row.ServiceId },
                { "versionLabel", row.VersionLabel },
                { "description", row.Description },
                { "createdAt", row.CreatedAt },
                { "updatedAt", row.UpdatedAt },
            }
        };

        await connection.OpenAsync();

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(params Guid[] ids)
    {
        const string sql = @"
DELETE FROM public.config_versions
WHERE id = ANY(:ids);
";
        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(sql, connection) { Parameters = { { "ids", ids }, } };

        await connection.OpenAsync();

        await command.ExecuteNonQueryAsync();
    }

    public Guid Track(Guid entity)
    {
        _ids.Add(entity);
        return entity;
    }

    public ValueTask DisposeAsync() => new(DeleteAsync([.. _ids]));

    public sealed class TableRow
    {
        public Guid Id { get; set; }
        public Guid ServiceId { get; set; }
        public string VersionLabel { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
