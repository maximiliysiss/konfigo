using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Models;

public sealed record SetEntryRequest(ServiceId ServiceId, VersionId VersionId, SetEntryRequest.SetRequest[] Requests, UserDto UpdatedBy)
{
    public sealed record SetRequest(EntryId Id, string? RawValue, int Generation);
}
