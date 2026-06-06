using System;
using Konfigo.Domain.Enums;
using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Notifications.Models;

public sealed record NotificationRequest(ServiceId ServiceId, VersionId VersionId, NotificationRequest.Request[] Requests)
{
    public sealed record Request(EntryId EntryId, string Key, ConfigValueType Type, string? RawValue, int Generation, DateTime Timestamp);
}
