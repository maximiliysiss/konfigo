using Konfigo.Abstraction.Attributes;

namespace Konfigo.Sample.Options;

[ConfigGroup]
public sealed class RealtimeConfigOptions
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

    public enum ServiceType
    {
        Unknown,
        Web,
        Mobile,
        Desktop
    }
}
