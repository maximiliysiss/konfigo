using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Models;

public sealed record CreateVersionRequest(
    ServiceId ServiceId,
    string VersionLabel,
    string? Description,
    User CreatedBy);
