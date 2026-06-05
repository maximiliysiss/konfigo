using Konfigo.Abstraction.Attributes;

namespace Konfigo.Client.IntegrationTests.Shared.Options;

[ConfigGroup]
public class ReactiveChangedOptions
{
    [ConfigKey(defaultValue: "42")]
    public int Value { get; set; } = 42;
}
