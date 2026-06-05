namespace Konfigo.Domain.ValueType;

public sealed record UpdateEntryRequest(
    string? RawValue,
    string? EnumDefinition,
    string? Description,
    string? GroupName,
    string? GroupDescription,
    int Generation);
