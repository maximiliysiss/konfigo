using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Konfigo.IntegrationTests.Shared.Extensions;

public static class HttpClientJsonExtensions
{
    private static readonly JsonSerializerOptions _requestJsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly JsonSerializerOptions _responseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static Task<HttpResponseMessage> PostAsKonfigoJsonAsync<TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest request) =>
        client.PostAsJsonAsync(requestUri, request, _requestJsonOptions);

    public static Task<HttpResponseMessage> PutAsKonfigoJsonAsync<TRequest>(
        this HttpClient client,
        string requestUri,
        TRequest request) =>
        client.PutAsJsonAsync(requestUri, request, _requestJsonOptions);

    public static async Task<TResponse?> ReadKonfigoJsonAsync<TResponse>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(content)
            ? default
            : JsonSerializer.Deserialize<TResponse>(content, _responseJsonOptions);
    }

    public static async Task<TResponse> ReadRequiredKonfigoJsonAsync<TResponse>(this HttpResponseMessage response)
    {
        var result = await response.ReadKonfigoJsonAsync<TResponse>();
        return result ?? throw new JsonException($"Response body cannot be deserialized to {typeof(TResponse).Name}.");
    }
}
