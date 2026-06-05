using System;
using System.Collections.Generic;
using Konfigo.Domain.Shared;
using Konfigo.Domain.ValueType;

namespace Konfigo.Domain.Entities;

public sealed class ConfigVersion : EntityBase<VersionId>
{
    public required ServiceId ServiceId { get; set; }
    public required string VersionLabel { get; set; }
    public string? Description { get; set; }
    public ICollection<ConfigEntry> ConfigEntries { get; set; } = [];

    public void Update(UpdateVersionRequest request, DateTimeOffset now)
    {
        VersionLabel = request.VersionLabel;
        Description = request.Description;
        UpdatedAt = now;
    }
}
