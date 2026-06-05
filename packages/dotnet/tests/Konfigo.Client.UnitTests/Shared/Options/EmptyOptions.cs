using Konfigo.Abstraction.Attributes;

namespace Konfigo.Client.UnitTests.Shared.Options;

[ConfigGroup]
public class EmptyOptions
{
    public int NonOptionProp { get; set; }
}
