using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.IntegrationTests.DbHelpers.Shared;
using Konfigo.IntegrationTests.Shared.Npgsql;
using Npgsql;

namespace Konfigo.IntegrationTests.DbHelpers;

internal sealed class ApplicationServiceDbHelper : IDbHelper, ITracker<Guid>
{
    private readonly HashSet<Guid> _ids = [];

    private readonly IConnectionFactory _connectionFactory;

    public ApplicationServiceDbHelper(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<TableRow?> GetAsync(Guid id)
    {
        const string sql = @"
SELECT id, name, description, repository_url, gitlab_project_id, contact_email,
       created_at, updated_at
FROM public.application_services
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
            ContactEmail = reader.GetNullableString("contact_email"),
            GitLabProjectId = reader.GetNullableString("gitlab_project_id"),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
            Description = reader.GetNullableString("description"),
            RepositoryUrl = reader.GetNullableString("repository_url"),
            Name = reader.GetString("name"),
        };
    }

    public async Task InsertAsync(TableRow row)
    {
        const string sql = @"
INSERT INTO public.application_services
    (id, name, description, repository_url, gitlab_project_id, contact_email, created_at, updated_at)
VALUES
    (:id, :name, :description, :repositoryUrl, :gitLabProjectId, :contactEmail, :createdAt, :updatedAt);
";
        _ids.Add(row.Id);

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(sql, connection)
        {
            Parameters =
            {
                { "id", row.Id },
                { "name", row.Name },
                { "description", row.Description },
                { "repositoryUrl", row.RepositoryUrl },
                { "gitLabProjectId", row.GitLabProjectId },
                { "contactEmail", row.ContactEmail },
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
DELETE FROM public.application_services
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
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RepositoryUrl { get; set; }
        public string? GitLabProjectId { get; set; }
        public string? ContactEmail { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
