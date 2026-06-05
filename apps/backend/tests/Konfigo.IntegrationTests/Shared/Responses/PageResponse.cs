namespace Konfigo.IntegrationTests.Shared.Responses;

public sealed class PageResponse<T>
{
    public T[] Entities { get; set; } = [];
    public string? NextPageToken { get; set; }
}
