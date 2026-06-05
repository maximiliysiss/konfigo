using System;
using Konfigo.Domain.Enums;
using Konfigo.Domain.Shared;
using Konfigo.Domain.ValueType;

namespace Konfigo.Domain.Entities;

public sealed class ConfigEntry : EntityBase<EntryId>
{
    public VersionId ConfigVersionId { get; set; }
    public ConfigVersion ConfigVersion { get; set; } = null!;
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string? RawValue { get; set; }
    public ConfigValueType ValueType { get; set; }
    public string? EnumDefinition { get; set; }
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public string? GroupDescription { get; set; }
    public required int Generation { get; set; }

    public void Update(UpdateEntryRequest request, DateTimeOffset now)
    {
        if (request.Generation != Generation)
        {
            throw new InvalidOperationException("Generation mismatch");
        }

        Generation++;
        RawValue = request.RawValue;
        EnumDefinition = request.EnumDefinition;
        Description = request.Description;
        GroupName = request.GroupName;
        GroupDescription = request.GroupDescription;
        UpdatedAt = now;
    }

    public void Set(string? rawValue, DateTimeOffset now, int generation)
    {
        if (generation != Generation)
        {
            throw new InvalidOperationException("Generation mismatch");
        }

        Generation++;
        RawValue = rawValue;
        UpdatedAt = now;
    }
}
