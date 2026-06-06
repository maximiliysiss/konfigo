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
                JsonValueKind.Object => AsKeyValuePairs(property.Value.EnumerateObject(), localKey),
                JsonValueKind.Array => AsKeyValuePairs(property.Value.EnumerateArray(), localKey),
                _ => [(localKey, property.Value.GetString())]
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
                JsonValueKind.Object => AsKeyValuePairs(node.EnumerateObject(), localKey),
                JsonValueKind.Array => AsKeyValuePairs(node.EnumerateArray(), localKey),
                _ => [(localKey, node.GetString())]
            };

            foreach (var tuple in values)
                yield return tuple;
        }
    }
}
