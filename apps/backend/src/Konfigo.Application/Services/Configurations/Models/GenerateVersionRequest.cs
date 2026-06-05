using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Models;

public sealed record GenerateVersionRequest(
    ServiceId ServiceId,
    string VersionLabel,
    string? Description,
    GenerateVersionRequest.EntryRequest[] Entries)
{
    public sealed record EntryRequest(
        string Key,
        string Name,
        string? RawValue,
        ConfigValueType ValueType,
        string? EnumDefinition,
        string? Description,
        string? GroupName,
        string? GroupDescription);
}
