using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Models;

public sealed record UpdateVersionRequest(
    ServiceId ServiceId,
    VersionId VersionId,
    string VersionLabel,
    string? Description,
    UserId UpdatedBy);
