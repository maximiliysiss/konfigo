using System;
using Konfigo.Domain.Enums;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Grpc.Converters;

internal static class ValueTypeConverter
{
    public static ValueType ToProto(this ConfigValueType type)
    {
        return type switch
        {
            ConfigValueType.String => ValueType.String,
            ConfigValueType.DateTime => ValueType.DateTime,
            ConfigValueType.TimeSpan => ValueType.TimeSpan,
            ConfigValueType.Json => ValueType.Json,
            ConfigValueType.Enum => ValueType.Enum,
            ConfigValueType.Number => ValueType.Number,
            ConfigValueType.Boolean => ValueType.Boolean,
            ConfigValueType.Array => ValueType.Array,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public static ConfigValueType ToDomain(this ValueType type)
    {
        return type switch
        {
            ValueType.String => ConfigValueType.String,
            ValueType.DateTime => ConfigValueType.DateTime,
            ValueType.TimeSpan => ConfigValueType.TimeSpan,
            ValueType.Json => ConfigValueType.Json,
            ValueType.Enum => ConfigValueType.Enum,
            ValueType.Number => ConfigValueType.Number,
            ValueType.Boolean => ConfigValueType.Boolean,
            ValueType.Array => ConfigValueType.Array,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
