using System;

namespace Konfigo.Client.Options;

internal sealed class RealtimeConfigOptions
{
    /// <summary>
    /// If true, the Realtime config provider will be enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// The version of the service
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The service id
    /// </summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>
    /// The version id. Created when the service is created.
    /// </summary>
    public string VersionId { get; set; } = string.Empty;

    /// <summary>
    /// The url of the realtime config service
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the last request.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// The interval between requests if the service is down or there is an error.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The initial timeout for the first request.
    /// </summary>
    public TimeSpan InitialRequestDelay { get; set; } = TimeSpan.FromSeconds(10);
}
