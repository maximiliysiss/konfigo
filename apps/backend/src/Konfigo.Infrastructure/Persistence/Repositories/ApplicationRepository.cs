using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Application.Repositories.Models;
using Konfigo.Domain.Entities;
using Konfigo.Domain.ValueType;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Infrastructure.Persistence.Npgsql;
using Npgsql;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class ApplicationRepository(IConnectionFactory connectionFactory) : IApplicationsRepository
{
    public async IAsyncEnumerable<ApplicationService> GetAsync(
        SearchServiceRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string query = @"
SELECT id, created_at, updated_at, name, description, repository_url, contact_email, members, num
FROM public.application_services
WHERE (cardinality(:ids) = 0 OR id = ANY(:ids))
  AND (:name IS NULL OR name LIKE '%' || :name || '%')
  AND (:member_id IS NULL OR array[:member_id, :member_email] && members)
  AND num < :cursorNum
ORDER BY num DESC
LIMIT :pageSize;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var ids = request.Ids.Select(x => x.Value).ToArray();

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "ids", ids },
                { "name", string.IsNullOrWhiteSpace(request.Name) ? null : request.Name },
                { "member_id", request.Member?.Id.Value },
                { "member_email", request.Member?.Email },
                { "cursorNum", request.Cursor.Num },
                { "pageSize", request.PageSize },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return Map(reader);
        }
    }

    public async Task<ApplicationService> AddAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        const string query = @"
INSERT INTO public.application_services (id, created_at, updated_at, name, description, repository_url, contact_email, members)
VALUES (:id, :createdAt, :updatedAt, :name, :description, :repositoryUrl, :contactEmail, :members)
RETURNING num;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "id", service.Id.Value },
                { "createdAt", service.CreatedAt },
                { "updatedAt", service.UpdatedAt },
                { "name", service.Name },
                { "description", service.Description },
                { "repositoryUrl", service.RepositoryUrl },
                { "contactEmail", service.ContactEmail },
                { "members", service.Members.Select(x => x.Value).ToArray() },
            }
        };

        await connection.OpenAsync(cancellationToken);

        service.Num = (int)(await command.ExecuteScalarAsync(cancellationToken))!;

        return service;
    }

    public async Task UpdateAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        const string query = @"
UPDATE public.application_services
SET updated_at = :updatedAt,
    name = :name,
    description = :description,
    repository_url = :repositoryUrl,
    contact_email = :contactEmail,
    members = :members
WHERE id = :id;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "id", service.Id.Value },
                { "updatedAt", service.UpdatedAt },
                { "name", service.Name },
                { "description", service.Description },
                { "repositoryUrl", service.RepositoryUrl },
                { "contactEmail", service.ContactEmail },
                { "members", service.Members.Select(x => x.Value).ToArray() },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(ApplicationService service, CancellationToken cancellationToken)
    {
        const string query = @"
DELETE FROM public.application_services
WHERE id = :id;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "id", service.Id.Value },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static ApplicationService Map(DbDataReader reader)
    {
        return new ApplicationService
        {
            Id = new ServiceId(reader.GetFieldValue<Guid>("id")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
            Num = reader.GetInt32("num"),
            Name = reader.GetString("name"),
            Description = reader.GetNullableString("description"),
            RepositoryUrl = reader.GetNullableString("repository_url"),
            ContactEmail = reader.GetNullableString("contact_email"),
            Members = reader.GetFieldValue<string[]>("members").Select(x => new UserId(x)).ToHashSet(),
        };
    }
}
