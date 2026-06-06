using Konfigo.Abstraction.Attributes;

namespace Konfigo.Sample.Options;

[ConfigGroup]
public sealed class TestOptions
{
    [ConfigKey(DefaultValue = "false")]
    public bool IsEnabled { get; set; }

    [ConfigKey(DefaultValue = "version")]
    public string Version { get; set; } = "version";

    [ConfigKey(DefaultValue = "00000000-0000-0000-0000-000000000000")]
    public Guid ServiceId { get; set; }

    [ConfigKey]
    public string Url { get; set; } = string.Empty;

    [ConfigKey(DefaultValue = "Web")]
    public ServiceType Type { get; set; } = ServiceType.Web;

    [ConfigKey(DefaultValue = "[1,2,3]")]
    public int[] Numbers { get; set; } = [];

    [ConfigKey(DefaultValue = "{\"1\":1,\"2\":2}")]
    public Dictionary<int, int> Values { get; set; } = [];

    [ConfigKey(DefaultValue = "{\"Value\":0,\"Str\":\"InnerOptions\"}")]
    public InnerOptions Inner { get; set; } = new InnerOptions();

    public enum ServiceType
    {
        Unknown,
        Web,
        Mobile,
        Desktop
    }

    public sealed class InnerOptions
    {
        public int Value { get; set; }
        public string Str { get; set; } = "InnerOptions";
    }
}
