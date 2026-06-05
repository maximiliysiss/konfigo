using System;

namespace Konfigo.Application.Services.Configurations.Options;

public sealed class ConfigVersionServiceOptions
{
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
