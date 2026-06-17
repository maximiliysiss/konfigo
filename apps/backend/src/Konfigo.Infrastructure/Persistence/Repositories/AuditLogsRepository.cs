using System;
using System.Collections.Generic;
using System.Data.Common;
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
        const string query = @"
INSERT INTO public.audit_logs (id, created_at, updated_at, service_id, user_id, entry)
VALUES (:id, :createdAt, :updatedAt, :serviceId, :userId, :entry)
RETURNING num;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var log in auditLog)
        {
            await using DbCommand command = new DbCommandInitializer(query, connection, transaction)
            {
                Parameters =
                {
                    { "id", log.Id.Value },
                    { "createdAt", log.CreatedAt },
                    { "updatedAt", log.UpdatedAt },
                    { "serviceId", log.ServiceId.Value },
                    { "userId", log.UserId?.Value },
                    { "entry", JsonSerializer.Serialize(log.Entry, _jsonOptions), NpgsqlDbType.Jsonb },
                }
            };

            log.Num = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        await transaction.CommitAsync(cancellationToken);
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
