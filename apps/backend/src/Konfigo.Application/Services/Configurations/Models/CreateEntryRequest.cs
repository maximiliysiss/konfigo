using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Models;

public sealed record CreateEntryRequest(
    ServiceId ServiceId,
    VersionId VersionId,
    string Key,
    string Name,
    string? RawValue,
    ConfigValueType ValueType,
    string? EnumDefinition,
    string? Description,
    string? GroupName,
    string? GroupDescription,
    UserId CreatedBy);
