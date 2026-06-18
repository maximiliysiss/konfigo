using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Models;

public sealed record DeleteEntryRequest(
    ServiceId ServiceId,
    VersionId VersionId,
    EntryId Id,
    User DeletedBy);
