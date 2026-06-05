namespace Konfigo.Controllers.Models.Entry;

public sealed class UpdateConfigEntryRequest
{
    public string? RawValue { get; set; }
    public string? EnumDefinition { get; set; }
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public string? GroupDescription { get; set; }
    public int Generation { get; set; }
}
