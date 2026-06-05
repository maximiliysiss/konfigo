namespace Konfigo.Domain.ValueType;

public sealed record UpdateServiceRequest(
    string Name,
    string? Description,
    string? RepositoryUrl,
    string? GitLabProjectId,
    string? ContactEmail);
