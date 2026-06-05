namespace Konfigo.Controllers.Models.Shared;

public sealed class PageResponse<T>
{
    public static readonly PageResponse<T> Empty = new() { Entities = [], NextPageToken = null };

    public required T[] Entities { get; set; }
    public required string? NextPageToken { get; set; }
}
