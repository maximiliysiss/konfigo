using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Repositories.Models;

public sealed record SearchAuditLogRequest(
    ServiceId ServiceId,
    int PageSize,
    SearchAuditLogRequest.PageToken Cursor)
{
    public static SearchAuditLogRequest Create(
        ServiceId serviceId,
        int? pageSize = null,
        PageToken? cursor = null)
    {
        return new SearchAuditLogRequest(
            ServiceId: serviceId,
            PageSize: pageSize ?? int.MaxValue,
            Cursor: cursor ?? PageToken.Empty);
    }

    public record struct PageToken(int Num)
    {
        public static readonly PageToken Empty = new(int.MaxValue);
    }
}
