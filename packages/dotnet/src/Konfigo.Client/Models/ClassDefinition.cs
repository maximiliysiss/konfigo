using System;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.Models;

internal sealed record ClassDefinition(
    string Key,
    string Name,
    string? Description,
    ClassDefinition.OptionDefinition[] Options,
    Type Type)
{
    public sealed record OptionDefinition(
        string Key,
        string Name,
        string? Description,
        ValueType Type,
        string? DefaultValue,
        string[]? EnumValues);
}
