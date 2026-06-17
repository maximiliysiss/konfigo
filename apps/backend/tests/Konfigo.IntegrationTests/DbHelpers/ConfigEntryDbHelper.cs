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

internal sealed class ConfigEntryDbHelper : IDbHelper, ITracker<Guid>
{
    private readonly HashSet<Guid> _ids = [];

    private readonly IConnectionFactory _connectionFactory;

    public ConfigEntryDbHelper(IConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<TableRow?> GetAsync(Guid id)
    {
        const string sql = @"
SELECT id, config_version_id, key, name, raw_value, value_type, enum_definition,
       description, group_name, group_description, generation, created_at, updated_at
FROM public.config_entries
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
            ConfigVersionId = reader.GetFieldValue<Guid>("config_version_id"),
            Key = reader.GetString("key"),
            Name = reader.GetString("name"),
            RawValue = reader.GetNullableString("raw_value"),
            ValueType = reader.GetInt32("value_type"),
            EnumDefinition = reader.GetNullableString("enum_definition"),
            Description = reader.GetNullableString("description"),
            GroupName = reader.GetNullableString("group_name"),
            GroupDescription = reader.GetNullableString("group_description"),
            Generation = reader.GetInt32("generation"),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
        };
    }

    public async Task InsertAsync(TableRow row)
    {
        const string sql = @"
INSERT INTO public.config_entries
    (id, config_version_id, key, name, raw_value, value_type, enum_definition,
     description, group_name, group_description, generation, created_at, updated_at)
VALUES
    (:id, :configVersionId, :key, :name, :rawValue, :valueType, :enumDefinition,
     :description, :groupName, :groupDescription, :generation, :createdAt, :updatedAt);
";
        _ids.Add(row.Id);

        await using var connection = await _connectionFactory.GetConnectionAsync(CancellationToken.None);

        await using DbCommand command = new DbCommandInitializer(sql, connection)
        {
            Parameters =
            {
                { "id", row.Id },
                { "configVersionId", row.ConfigVersionId },
                { "key", row.Key },
                { "name", row.Name },
                { "rawValue", row.RawValue },
                { "valueType", row.ValueType },
                { "enumDefinition", row.EnumDefinition },
                { "description", row.Description },
                { "groupName", row.GroupName },
                { "groupDescription", row.GroupDescription },
                { "generation", row.Generation },
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
DELETE FROM public.config_entries
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
        public Guid ConfigVersionId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? RawValue { get; set; }
        public int ValueType { get; set; }
        public string? EnumDefinition { get; set; }
        public string? Description { get; set; }
        public string? GroupName { get; set; }
        public string? GroupDescription { get; set; }
        public int Generation { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
