using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Repositories.Models;

public sealed record SearchAuditLogRequest(
    ServiceId ServiceId,
    int PageSize,
    SearchAuditLogRequest.PageToken Cursor,
    bool AsTracking)
{
    public static SearchAuditLogRequest Create(
        ServiceId serviceId,
        int? pageSize = null,
        PageToken? cursor = null,
        bool asTracking = true)
    {
        return new SearchAuditLogRequest(
            ServiceId: serviceId,
            PageSize: pageSize ?? int.MaxValue,
            Cursor: cursor ?? PageToken.Empty,
            AsTracking: asTracking);
    }

    public record struct PageToken(int Num)
    {
        public static readonly PageToken Empty = new(int.MaxValue);
    }
}
