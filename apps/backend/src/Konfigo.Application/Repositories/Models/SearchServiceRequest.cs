using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Repositories.Models;

public sealed record SearchServiceRequest(
    ServiceId[] Ids,
    string? Name,
    UserId? Member,
    int PageSize,
    SearchServiceRequest.PageToken Cursor,
    bool AsTracking)
{
    public static SearchServiceRequest Create(
        ServiceId[]? ids = null,
        string? name = null,
        UserId? member = null,
        int? pageSize = null,
        PageToken? cursor = null,
        bool asTracking = true)
    {
        return new SearchServiceRequest(
            Ids: ids ?? [],
            Name: name,
            Member: member,
            PageSize: pageSize ?? int.MaxValue,
            Cursor: cursor ?? PageToken.Empty,
            AsTracking: asTracking);
    }

    public record struct PageToken(int Num)
    {
        public static readonly PageToken Empty = new(Num: int.MaxValue);
    }
}
