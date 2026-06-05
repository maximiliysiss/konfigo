using System;
using System.Text.Json;

namespace Konfigo.Controllers.Converters;

internal static class PageTokenConverter
{
    public static T AsPageToken<T>(this string? value, T defaultValue)
    {
        return string.IsNullOrEmpty(value)
            ? defaultValue
            : JsonSerializer.Deserialize<T>(Convert.FromBase64String(value)) ?? defaultValue;
    }

    public static string? AsBase64<T>(this T? value)
    {
        return value is null
            ? null
            : Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value));
    }
}
