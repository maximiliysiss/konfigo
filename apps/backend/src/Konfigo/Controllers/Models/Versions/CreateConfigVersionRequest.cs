namespace Konfigo.Controllers.Models.Versions;

public sealed class CreateConfigVersionRequest
{
    public string VersionLabel { get; set; } = string.Empty;
    public string? Description { get; set; }
}
