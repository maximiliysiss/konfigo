namespace Konfigo.Controllers.Models.Services;

public sealed class CreateOrUpdateServiceRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RepositoryUrl { get; set; }
    public string? GitLabProjectId { get; set; }
    public string? ContactEmail { get; set; }
}
