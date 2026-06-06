using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Konfigo.Client.Extensions;

internal static class JsonDocumentExtensions
{
    public static IEnumerable<(string, string?)> AsKeyValuePairs(this JsonDocument document, string key)
    {
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Array => AsKeyValuePairs(document.RootElement.EnumerateArray(), key),
            JsonValueKind.Object => AsKeyValuePairs(document.RootElement.EnumerateObject(), key),
            _ => []
        };
    }

    private static IEnumerable<(string, string?)> AsKeyValuePairs(JsonElement.ObjectEnumerator obj, string key)
    {
        foreach (var property in obj)
        {
            var localKey = $"{key}:{property.Name}";

            var values = property.Value.ValueKind switch
            {
                JsonValueKind.Array => AsKeyValuePairs(property.Value.EnumerateArray(), localKey),
                JsonValueKind.Object => AsKeyValuePairs(property.Value.EnumerateObject(), localKey),
                _ => [(localKey, GetValue(property.Value))]
            };

            foreach (var tuple in values)
                yield return tuple;
        }
    }

    private static IEnumerable<(string, string?)> AsKeyValuePairs(JsonElement.ArrayEnumerator array, string key)
    {
        foreach (var (node, i) in array.Select((c, i) => (c, i)))
        {
            var localKey = $"{key}:{i}";

            var values = node.ValueKind switch
            {
                JsonValueKind.Array => AsKeyValuePairs(node.EnumerateArray(), localKey),
                JsonValueKind.Object => AsKeyValuePairs(node.EnumerateObject(), localKey),
                _ => [(localKey, GetValue(node))]
            };

            foreach (var tuple in values)
                yield return tuple;
        }
    }

    private static string? GetValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => element.GetString(),
            _ => element.GetRawText()
        };
    }
}
