using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Konfigo.Abstraction.Attributes;
using Konfigo.Client.Extensions;
using Konfigo.Client.Models;
using ValueType = Konfigo.Client.Grpc.ValueType;

namespace Konfigo.Client.Infrastructure.Extensions;

internal static class Assemblies
{
    private static readonly ClassDefinition[] _definitions;

    static Assemblies()
    {
        _definitions = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(c => c.GetTypes())
            .Where(c => c is { IsClass: true, IsAbstract: false } && c.HasCustomAttribute<ConfigGroupAttribute>())
            .Select(MapClass)
            .Where(c => c.Options is not [])
            .ToArray();

        return;

        static ClassDefinition MapClass(Type type)
        {
            var configGroupAttribute = type.GetCustomAttribute<ConfigGroupAttribute>();
            var key = configGroupAttribute?.Key ?? type.Name;

            var options = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(static property => property.GetMethod is not null && property.HasCustomAttribute<ConfigKeyAttribute>())
                .Select(x => MapProperty(key, x))
                .ToArray();

            return new ClassDefinition(
                Key: key,
                Type: type,
                Name: configGroupAttribute?.GroupName ?? type.Name,
                Description: configGroupAttribute?.Description,
                Options: options);
        }

        static ClassDefinition.OptionDefinition MapProperty(string className, PropertyInfo property)
        {
            var configKeyAttribute = property.GetCustomAttribute<ConfigKeyAttribute>();

            var type = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            var valueType = MapType(underlyingType);

            return new ClassDefinition.OptionDefinition(
                Key: $"{className}:{property.Name}",
                Name: configKeyAttribute?.Name ?? property.Name,
                Description: configKeyAttribute?.Description,
                Type: valueType,
                DefaultValue: configKeyAttribute?.DefaultValue ?? MapDefaultValue(valueType, type),
                EnumValues: underlyingType.IsEnum ? underlyingType.GetEnumNames() : null);
        }

        static string? MapDefaultValue(ValueType valueType, Type type)
        {
            if (Nullable.GetUnderlyingType(type) is not null)
            {
                return null;
            }

            return valueType switch
            {
                ValueType.Array => "[]",
                ValueType.String => string.Empty,
                ValueType.DateTime => type switch
                {
                    _ when type == typeof(DateTimeOffset) => DateTimeOffset.MinValue.ToString(CultureInfo.InvariantCulture),
                    _ => DateTime.MinValue.ToString(CultureInfo.InvariantCulture),
                },
                ValueType.TimeSpan => "00:00:00",
                ValueType.Json => "{}",
                ValueType.Enum => type.GetEnumNames()[0],
                ValueType.Number => "0",
                ValueType.Boolean => "false",
                _ => null,
            };
        }

        static ValueType MapType(Type type)
        {
            return type switch
            {
                _ when type == typeof(string) => ValueType.String,
                _ when type == typeof(bool) => ValueType.Boolean,
                _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => ValueType.DateTime,
                _ when type == typeof(TimeSpan) => ValueType.TimeSpan,
                _ when type.IsEnum => ValueType.Enum,
                _ when type.IsNumber() => ValueType.Number,
                _ when type.IsArray => ValueType.Array,
                _ => ValueType.Json,
            };
        }
    }

    public static ClassDefinition[] GetDefinitions() => _definitions;
}
