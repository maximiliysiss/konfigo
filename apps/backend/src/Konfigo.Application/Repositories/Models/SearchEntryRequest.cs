using System;
using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Repositories.Models;

public sealed record SearchEntryRequest(
    ServiceId ServiceId,
    VersionId VersionId,
    EntryId[] Ids,
    DateTimeOffset? From,
    bool AsTracking)
{
    public static SearchEntryRequest Create(
        ServiceId serviceId,
        VersionId versionId,
        EntryId[]? ids = null,
        DateTimeOffset? from = null,
        bool asTracking = true)
    {
        return new SearchEntryRequest(
            ServiceId: serviceId,
            VersionId: versionId,
            From: from,
            Ids: ids ?? [],
            AsTracking: asTracking);
    }
}
