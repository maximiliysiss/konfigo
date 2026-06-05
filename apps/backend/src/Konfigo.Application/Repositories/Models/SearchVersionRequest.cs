using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Repositories.Models;

public sealed record SearchVersionRequest(
    ServiceId ServiceId,
    VersionId[] Ids,
    string? Label,
    int Limit,
    EEntityType[] Include,
    bool AsTracking)
{
    public static SearchVersionRequest Create(
        ServiceId serviceId,
        VersionId[]? ids = null,
        string? label = null,
        int limit = int.MaxValue,
        EEntityType[]? include = null,
        bool asTracking = true)
    {
        return new SearchVersionRequest(
            ServiceId: serviceId,
            Ids: ids ?? [],
            Label: label,
            Limit: limit,
            Include: include ?? [],
            AsTracking: asTracking);
    }
}
