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

internal sealed class ConfigVersionsRepository(IConnectionFactory connectionFactory) : IConfigVersionsRepository
{
    public async IAsyncEnumerable<ConfigVersion> GetAsync(
        SearchVersionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (request.Include.Contains(EEntityType.Entry))
        {
            var versions = await GetWithEntriesAsync(request, cancellationToken);

            foreach (var version in versions)
            {
                yield return version;
            }

            yield break;
        }

        const string query = @"
SELECT id, created_at, updated_at, service_id, version_label, description
FROM public.config_versions
WHERE service_id = :serviceId
  AND (cardinality(:ids) = 0 OR id = ANY(:ids))
  AND (:label IS NULL OR version_label = :label)
ORDER BY created_at DESC
LIMIT :limit;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var ids = request.Ids.Select(x => x.Value).ToArray();

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "serviceId", request.ServiceId.Value },
                { "ids", ids },
                { "label", string.IsNullOrEmpty(request.Label) ? null : request.Label },
                { "limit", request.Limit },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return MapVersion(reader);
        }
    }

    public async Task<ConfigVersion> AddAsync(ConfigVersion version, CancellationToken cancellationToken)
    {
        const string versionQuery = @"
INSERT INTO public.config_versions (id, created_at, updated_at, service_id, version_label, description)
VALUES (:id, :createdAt, :updatedAt, :serviceId, :versionLabel, :description);
";

        const string entryQuery = @"
INSERT INTO public.config_entries
    (id, created_at, updated_at, config_version_id, key, name, raw_value, value_type, enum_definition,
     description, group_name, group_description, generation)
VALUES
    (:id, :createdAt, :updatedAt, :configVersionId, :key, :name, :rawValue, :valueType, :enumDefinition,
     :description, :groupName, :groupDescription, :generation);
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(versionQuery, connection, transaction)
        {
            Parameters =
            {
                { "id", version.Id.Value },
                { "createdAt", version.CreatedAt },
                { "updatedAt", version.UpdatedAt },
                { "serviceId", version.ServiceId.Value },
                { "versionLabel", version.VersionLabel },
                { "description", version.Description },
            }
        };

        await command.ExecuteNonQueryAsync(cancellationToken);

        foreach (var entry in version.ConfigEntries)
        {
            await using DbCommand entryCommand = new DbCommandInitializer(entryQuery, connection, transaction)
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

            await entryCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return version;
    }

    public async Task UpdateAsync(ConfigVersion version, CancellationToken cancellationToken)
    {
        const string query = @"
UPDATE public.config_versions
SET updated_at = :updatedAt,
    version_label = :versionLabel,
    description = :description
WHERE id = :id;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "id", version.Id.Value },
                { "updatedAt", version.UpdatedAt },
                { "versionLabel", version.VersionLabel },
                { "description", version.Description },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ConfigVersion>> GetWithEntriesAsync(
        SearchVersionRequest request,
        CancellationToken cancellationToken)
    {
        const string query = @"
SELECT cv.id, cv.created_at, cv.updated_at, cv.service_id, cv.version_label, cv.description,
       ce.id AS entry_id, ce.created_at AS entry_created_at, ce.updated_at AS entry_updated_at,
       ce.config_version_id AS entry_config_version_id, ce.key AS entry_key, ce.name AS entry_name,
       ce.raw_value AS entry_raw_value, ce.value_type AS entry_value_type,
       ce.enum_definition AS entry_enum_definition, ce.description AS entry_description,
       ce.group_name AS entry_group_name, ce.group_description AS entry_group_description,
       ce.generation AS entry_generation
FROM (
    SELECT id, created_at, updated_at, service_id, version_label, description
    FROM public.config_versions
    WHERE service_id = :serviceId
      AND (cardinality(:ids) = 0 OR id = ANY(:ids))
      AND (:label IS NULL OR version_label = :label)
    ORDER BY created_at DESC
    LIMIT :limit
) cv
LEFT JOIN public.config_entries ce ON ce.config_version_id = cv.id
ORDER BY cv.created_at DESC;
";

        await using var connection = await connectionFactory.GetConnectionAsync(cancellationToken);

        var ids = request.Ids.Select(x => x.Value).ToArray();

        await using DbCommand command = new DbCommandInitializer(query, connection)
        {
            Parameters =
            {
                { "serviceId", request.ServiceId.Value },
                { "ids", ids },
                { "label", string.IsNullOrEmpty(request.Label) ? null : request.Label },
                { "limit", request.Limit },
            }
        };

        await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var versions = new Dictionary<VersionId, ConfigVersion>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var id = new VersionId(reader.GetFieldValue<Guid>("id"));

            if (!versions.TryGetValue(id, out var version))
            {
                version = MapVersion(reader);
                versions.Add(id, version);
            }

            if (!reader.IsDBNull(reader.GetOrdinal("entry_id")))
            {
                version.ConfigEntries.Add(MapEntry(reader));
            }
        }

        return versions.Values;
    }

    private static ConfigVersion MapVersion(DbDataReader reader)
    {
        return new ConfigVersion
        {
            Id = new VersionId(reader.GetFieldValue<Guid>("id")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("updated_at"),
            ServiceId = new ServiceId(reader.GetFieldValue<Guid>("service_id")),
            VersionLabel = reader.GetString("version_label"),
            Description = reader.GetNullableString("description"),
        };
    }

    private static ConfigEntry MapEntry(DbDataReader reader)
    {
        return new ConfigEntry
        {
            Id = new EntryId(reader.GetFieldValue<Guid>("entry_id")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>("entry_created_at"),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset?>("entry_updated_at"),
            ConfigVersionId = new VersionId(reader.GetFieldValue<Guid>("entry_config_version_id")),
            Key = reader.GetString("entry_key"),
            Name = reader.GetString("entry_name"),
            RawValue = reader.GetNullableString("entry_raw_value"),
            ValueType = (ConfigValueType)reader.GetInt32("entry_value_type"),
            EnumDefinition = reader.GetNullableString("entry_enum_definition"),
            Description = reader.GetNullableString("entry_description"),
            GroupName = reader.GetNullableString("entry_group_name"),
            GroupDescription = reader.GetNullableString("entry_group_description"),
            Generation = reader.GetInt32("entry_generation"),
        };
    }
}
