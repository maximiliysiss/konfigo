using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Infrastructure.Persistence.Npgsql;
using Npgsql;
using NpgsqlTypes;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogsRepository(IConnectionFactory connectionFactory) : IAuditLogRepository
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };

    public async Task AddAsync(AuditLog[] auditLog, CancellationToken cancellationToken)
    {
        if (auditLog is [])
            return;

        const string query = @"
INSERT INTO public.audit_logs (id, created_at, updated_at, service_id, user_id, entry)
SELECT u.id, u.created_at, u.updated_at, u.service_id, u.user_id, u.entry
FROM UNNEST(
    :ids::uuid[],
    :createdAts::timestamptz[],
    :updatedAts::timestamptz[],
    :serviceIds::uuid[],
    :userIds::text[],
    :entries::jsonb[]
) AS u(id, created_at, updated_at, service_id, user_id, entry)
RETURNING id, num;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "ids", auditLog.Select(x => x.Id.Value).ToArray() },
                { "createdAts", auditLog.Select(x => x.CreatedAt).ToArray() },
                { "updatedAts", auditLog.Select(x => x.UpdatedAt).ToArray() },
                { "serviceIds", auditLog.Select(x => x.ServiceId.Value).ToArray() },
                { "userIds", auditLog.Select(x => x.UserId?.Value).ToArray() },
                { "entries", auditLog.Select(x => JsonSerializer.Serialize(x.Entry, _jsonOptions)).ToArray(), NpgsqlDbType.Array | NpgsqlDbType.Jsonb },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var logs = auditLog.ToDictionary(x => x.Id.Value);

        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetFieldValue<Guid>("id");
            logs[id].Num = reader.GetInt32("num");
        }
    }

    public async IAsyncEnumerable<AuditLog> GetAsync(
        SearchAuditLogRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string query = @"
SELECT id, created_at, updated_at, num, service_id, user_id, entry
FROM public.audit_logs
WHERE service_id = :serviceId
  AND num < :cursorNum
ORDER BY num DESC
LIMIT :pageSize;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "serviceId", request.ServiceId.Value },
                { "cursorNum", request.Cursor.Num },
                { "pageSize", request.PageSize },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new AuditLog
            {
                Id = new LogId(reader.GetFieldValue<Guid>("id")),
                CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
                UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
                Num = reader.GetInt32("num"),
                ServiceId = new ServiceId(reader.GetFieldValue<Guid>("service_id")),
                UserId = reader.GetNullableString("user_id") is { } userId ? new UserId(userId) : null,
                Entry = JsonSerializer.Deserialize<LogEntry>(reader.GetString("entry"), _jsonOptions)!,
            };
        }
    }
}
