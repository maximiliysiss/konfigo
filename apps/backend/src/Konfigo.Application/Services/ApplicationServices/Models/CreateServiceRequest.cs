using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.ApplicationServices.Models;

public sealed record CreateServiceRequest(
    string Name,
    string? Description,
    string? RepositoryUrl,
    string? GitLabProjectId,
    string? ContactEmail,
    UserId CreatedBy);
