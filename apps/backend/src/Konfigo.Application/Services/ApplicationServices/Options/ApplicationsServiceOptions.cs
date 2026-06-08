using System;

namespace Konfigo.Application.Services.ApplicationServices.Options;

internal sealed class ApplicationsServiceOptions
{
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
