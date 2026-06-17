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
using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;
using Konfigo.Infrastructure.Persistence.Factory;
using Konfigo.Infrastructure.Persistence.Npgsql;
using Npgsql;

namespace Konfigo.Infrastructure.Persistence.Repositories;

internal sealed class ConfigEntryRepository(IConnectionFactory connectionFactory) : IConfigEntryRepository
{
    public async IAsyncEnumerable<ConfigEntry> GetAsync(
        SearchEntryRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const string query = @"
SELECT ce.id, ce.created_at, ce.updated_at, ce.config_version_id, ce.key, ce.name, ce.raw_value, ce.value_type,
       ce.enum_definition, ce.description, ce.group_name, ce.group_description, ce.generation
FROM public.config_entries ce
INNER JOIN public.config_versions cv ON cv.id = ce.config_version_id
WHERE ce.config_version_id = :versionId
  AND cv.service_id = :serviceId
  AND (cardinality(:ids) = 0 OR ce.id = ANY(:ids))
  AND (:from IS NULL OR COALESCE(ce.updated_at, ce.created_at) >= :from);
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var ids = request.Ids.Select(x => x.Value).ToArray();

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "versionId", request.VersionId.Value },
                { "serviceId", request.ServiceId.Value },
                { "ids", ids },
                { "from", request.From },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return Map(reader);
        }
    }

    public async Task<ConfigEntry> AddAsync(ConfigEntry entry, CancellationToken cancellationToken)
    {
        const string query = @"
INSERT INTO public.config_entries
    (id, created_at, updated_at, config_version_id, key, name, raw_value, value_type, enum_definition,
     description, group_name, group_description, generation)
VALUES
    (:id, :createdAt, :updatedAt, :configVersionId, :key, :name, :rawValue, :valueType, :enumDefinition,
     :description, :groupName, :groupDescription, :generation);
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "id", entry.Id.Value },
                { "createdAt", entry.CreatedAt },
                { "updatedAt", entry.UpdatedAt },
                { "configVersionId", entry.ConfigVersionId.Value },
                { "key", entry.Key },
                { "name", entry.Name },
                { "rawValue", entry.RawValue },
                { "valueType", (int)entry.ValueType },
                { "enumDefinition", entry.EnumDefinition },
                { "description", entry.Description },
                { "groupName", entry.GroupName },
                { "groupDescription", entry.GroupDescription },
                { "generation", entry.Generation },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return entry;
    }

    public async Task UpdateAsync(ConfigEntry[] entry, CancellationToken cancellationToken)
    {
        if (entry is [])
            return;

        const string query = @"
UPDATE public.config_entries ce
SET updated_at = u.updated_at,
    raw_value = u.raw_value,
    enum_definition = u.enum_definition,
    description = u.description,
    group_name = u.group_name,
    group_description = u.group_description,
    generation = u.generation
FROM UNNEST(
    :ids::uuid[],
    :updatedAts::timestamptz[],
    :rawValues::text[],
    :enumDefinitions::text[],
    :descriptions::text[],
    :groupNames::text[],
    :groupDescriptions::text[],
    :generations::integer[]
) AS u(id, updated_at, raw_value, enum_definition, description, group_name, group_description, generation)
WHERE ce.id = u.id;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "ids", entry.Select(x => x.Id.Value).ToArray() },
                { "updatedAts", entry.Select(x => x.UpdatedAt).ToArray() },
                { "rawValues", entry.Select(x => x.RawValue).ToArray() },
                { "enumDefinitions", entry.Select(x => x.EnumDefinition).ToArray() },
                { "descriptions", entry.Select(x => x.Description).ToArray() },
                { "groupNames", entry.Select(x => x.GroupName).ToArray() },
                { "groupDescriptions", entry.Select(x => x.GroupDescription).ToArray() },
                { "generations", entry.Select(x => x.Generation).ToArray() },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(ConfigEntry entry, CancellationToken cancellationToken)
    {
        const string query = @"
DELETE FROM public.config_entries
WHERE id = :id;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "id", entry.Id.Value },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static ConfigEntry Map(DbDataReader reader)
    {
        return new ConfigEntry
        {
            Id = new EntryId(reader.GetFieldValue<Guid>("id")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
            ConfigVersionId = new VersionId(reader.GetFieldValue<Guid>("config_version_id")),
            Key = reader.GetString("key"),
            Name = reader.GetString("name"),
            RawValue = reader.GetNullableString("raw_value"),
            ValueType = (ConfigValueType)reader.GetInt32("value_type"),
            EnumDefinition = reader.GetNullableString("enum_definition"),
            Description = reader.GetNullableString("description"),
            GroupName = reader.GetNullableString("group_name"),
            GroupDescription = reader.GetNullableString("group_description"),
            Generation = reader.GetInt32("generation"),
        };
    }
}
