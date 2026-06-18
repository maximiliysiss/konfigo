using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.ApplicationServices.Models;

public sealed record UpdateServiceRequest(
    ServiceId Id,
    string Name,
    string? Description,
    string? RepositoryUrl,
    string? ContactEmail,
    User UpdatedBy);
