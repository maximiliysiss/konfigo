using System;
using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Updater.Models;

public sealed record ChangeEvent(ServiceId ServiceId, VersionId VersionId, ChangeEvent.Request[] Requests)
{
    public sealed record Request(EntryId EntryId, string Key, string? RawValue, int Generation, DateTime Timestamp);
}
