using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.IntegrationTests.DbHelpers.Shared;
using Npgsql;

namespace Konfigo.IntegrationTests.DbHelpers;

internal sealed class AuditLogDbHelper : IDbHelper, ITracker<Guid>
{
    private readonly HashSet<Guid> _ids = [];

    private readonly IConnectionFactory _connectionFactory;

    public AuditLogDbHelper(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<int> CountByServiceAsync(Guid serviceId)
    {
        const string sql = @"
SELECT count(*)
FROM public.audit_logs
WHERE service_id = :serviceId;
";
        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(sql, connection) { Parameters = { { "serviceId", serviceId }, } };

        await connection.OpenAsync();

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task DeleteByServiceAsync(Guid serviceId)
    {
        const string sql = @"
DELETE FROM public.audit_logs
WHERE service_id = :serviceId;
";
        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(sql, connection) { Parameters = { { "serviceId", serviceId }, } };

        await connection.OpenAsync();

        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(params Guid[] ids)
    {
        const string sql = @"
DELETE FROM public.audit_logs
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
}
