using Konfigo.Abstraction.Attributes;

namespace Konfigo.Client.IntegrationTests.Shared.Options;

[ConfigGroup(key: SectionName)]
public class CustomKeyOptions
{
    public const string SectionName = "custom-key-options";

    [ConfigKey(defaultValue: "7")]
    public int Value { get; set; } = 7;
}
