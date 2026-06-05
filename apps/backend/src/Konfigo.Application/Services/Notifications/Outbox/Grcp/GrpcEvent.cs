using System;
using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Notifications.Outbox.Grcp;

public sealed record GrpcEvent(ServiceId ServiceId, VersionId VersionId, GrpcEvent.Request[] Requests)
{
    public sealed record Request(EntryId EntryId, string Key, string? RawValue, int Generation, DateTime Timestamp);
}
