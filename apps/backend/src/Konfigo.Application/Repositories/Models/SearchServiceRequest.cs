using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Repositories.Models;

public sealed record SearchServiceRequest(
    ServiceId[] Ids,
    string? Name,
    User? Member,
    int PageSize,
    SearchServiceRequest.PageToken Cursor)
{
    public static SearchServiceRequest Create(
        ServiceId[]? ids = null,
        string? name = null,
        User? member = null,
        int? pageSize = null,
        PageToken? cursor = null)
    {
        return new SearchServiceRequest(
            Ids: ids ?? [],
            Name: name,
            Member: member,
            PageSize: pageSize ?? int.MaxValue,
            Cursor: cursor ?? PageToken.Empty);
    }

    public record struct PageToken(int Num)
    {
        public static readonly PageToken Empty = new(Num: int.MaxValue);
    }
}
