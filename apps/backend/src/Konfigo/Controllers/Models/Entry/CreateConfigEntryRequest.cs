using Konfigo.Domain.Enums;

namespace Konfigo.Controllers.Models.Entry;

public sealed class CreateConfigEntryRequest
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public ConfigValueType ValueType { get; set; }
    public string? EnumDefinition { get; set; }
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public string? GroupDescription { get; set; }
}
