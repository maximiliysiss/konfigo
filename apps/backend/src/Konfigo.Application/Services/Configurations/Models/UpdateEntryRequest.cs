using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Models;

public sealed record UpdateEntryRequest(
    ServiceId ServiceId,
    VersionId VersionId,
    EntryId Id,
    string? RawValue,
    string? EnumDefinition,
    string? Description,
    string? GroupName,
    string? GroupDescription,
    int Generation,
    User UpdatedBy);
