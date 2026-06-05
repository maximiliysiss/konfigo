using System;

namespace Konfigo.Controllers.Models.Entry;

public sealed class SetConfigEntryRequest
{
    public Guid Id { get; set; }
    public string? RawValue { get; set; }
    public int Generation { get; set; }
}
