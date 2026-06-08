using System;

namespace Konfigo.Application.Services.Configurations.Options;

internal sealed class ConfigVersionServiceOptions
{
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
