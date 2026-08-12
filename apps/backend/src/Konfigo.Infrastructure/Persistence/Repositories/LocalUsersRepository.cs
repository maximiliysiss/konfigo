using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Application.Repositories;
using Konfigo.Domain.Entities;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Infrastructure.Persistence.Npgsql;
using Npgsql;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class LocalUsersRepository(IConnectionFactory connectionFactory) : ILocalUsersRepository
{
    public async Task<LocalUser[]> GetAllAsync(CancellationToken cancellationToken)
    {
        const string query = @"
SELECT username, password_hash, role, created_at, updated_at
FROM public.local_users
ORDER BY username;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection);

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var users = new List<LocalUser>();

        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(Map(reader));
        }

        return users.ToArray();
    }

    public async Task<LocalUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        const string query = @"
SELECT username, password_hash, role, created_at, updated_at
FROM public.local_users
WHERE username = :username;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "username", username },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task AddAsync(LocalUser user, CancellationToken cancellationToken)
    {
        const string query = @"
INSERT INTO public.local_users (username, password_hash, role, created_at, updated_at)
VALUES (:username, :passwordHash, :role, :createdAt, :updatedAt);
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "username", user.Username },
                { "passwordHash", user.PasswordHash },
                { "role", user.Role },
                { "createdAt", user.CreatedAt },
                { "updatedAt", user.UpdatedAt },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(LocalUser user, CancellationToken cancellationToken)
    {
        const string query = @"
UPDATE public.local_users
SET password_hash = :passwordHash,
    role = :role,
    updated_at = :updatedAt
WHERE username = :username;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "username", user.Username },
                { "passwordHash", user.PasswordHash },
                { "role", user.Role },
                { "updatedAt", user.UpdatedAt },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(LocalUser user, CancellationToken cancellationToken)
    {
        const string query = @"
DELETE FROM public.local_users
WHERE username = :username;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "username", user.Username },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static LocalUser Map(DbDataReader reader)
    {
        return new LocalUser
        {
            Username = reader.GetString("username"),
            PasswordHash = reader.GetString("password_hash"),
            Role = reader.GetString("role"),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
        };
    }
}
