using Konfigo.Abstraction.Attributes;

namespace Konfigo.Client.IntegrationTests.Shared.Options;

[ConfigGroup]
public class SimpleOptions
{
    [ConfigKey]
    public int Value { get; set; } = 42;
}
