using System;
using Microsoft.Extensions.Logging;

namespace Konfigo.Client.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(1, LogLevel.Error, "Restarting service {hostedService} due to an exception. Retry #{retryNumber}.")]
    public static partial void RestartingService(this ILogger logger, string hostedService, int retryNumber, Exception exception);

    [LoggerMessage(2, LogLevel.Information, "Starting service {hostedService}.")]
    public static partial void StartingService(this ILogger logger, string hostedService);

    [LoggerMessage(3, LogLevel.Information, "Restarting service {hostedService} due to a configuration change.")]
    public static partial void ConfigurationChanged(this ILogger logger, string hostedService);

    [LoggerMessage(4, LogLevel.Information, "Stopping service {serviceName}.")]
    public static partial void StoppingService(this ILogger logger, string serviceName);

    [LoggerMessage(5, LogLevel.Warning, "Realtime config is disabled")]
    public static partial void RealtimeConfigDisabled(this ILogger logger);

    [LoggerMessage(6, LogLevel.Error, "Configuration is not available")]
    public static partial void ConfigurationNotAvailable(this ILogger logger);

    [LoggerMessage(7, LogLevel.Warning, "There are no assemblies to scan for realtime config")]
    public static partial void NoAssembliesToScan(this ILogger logger);

    [LoggerMessage(8, LogLevel.Error, "Realtime config provider is not available")]
    public static partial void RealtimeConfigProviderNotAvailable(this ILogger logger);

    [LoggerMessage(9, LogLevel.Information, "Realtime cycle config is starting")]
    public static partial void RealtimeCycleConfigStarting(this ILogger logger);

    [LoggerMessage(10, LogLevel.Information, "Received events batch from realtime config service")]
    public static partial void ReceivedEventsBatch(this ILogger logger);

    [LoggerMessage(11, LogLevel.Warning, "No events received")]
    public static partial void NoEventsReceived(this ILogger logger);

    [LoggerMessage(12, LogLevel.Information, "Received event from realtime config service with key = {key}")]
    public static partial void ReceivedEvent(this ILogger logger, string key);

    [LoggerMessage(13, LogLevel.Information, "Realtime config cycle finished with {count} updates")]
    public static partial void RealtimeConfigCycleFinished(this ILogger logger, int count);

    [LoggerMessage(14, LogLevel.Information, "Realtime config cycle is cancelled")]
    public static partial void RealtimeConfigCycleCancelled(this ILogger logger);

    [LoggerMessage(15, LogLevel.Error, "Error occurred during realtime config cycle")]
    public static partial void RealtimeConfigCycleError(this ILogger logger, Exception exception);

    [LoggerMessage(16, LogLevel.Information, "Realtime cycle config is finished")]
    public static partial void RealtimeCycleConfigFinished(this ILogger logger);

    [LoggerMessage(17, LogLevel.Debug, "Loading RealtimeConfig")]
    public static partial void LoadingRealtimeConfig(this ILogger logger);

    [LoggerMessage(18, LogLevel.Debug, "RealtimeConfig is loaded")]
    public static partial void RealtimeConfigLoaded(this ILogger logger);

    [LoggerMessage(19, LogLevel.Debug, "Setting RealtimeConfig")]
    public static partial void SettingRealtimeConfig(this ILogger logger);

    [LoggerMessage(20, LogLevel.Debug, "RealtimeConfig is set")]
    public static partial void RealtimeConfigSet(this ILogger logger);

    [LoggerMessage(21, LogLevel.Information, "Realtime config already exists")]
    public static partial void RealtimeConfigAlreadyExists(this ILogger logger);

    [LoggerMessage(22, LogLevel.Information, "Realtime config does not exist, creating")]
    public static partial void RealtimeConfigDoesNotExistCreating(this ILogger logger);

    [LoggerMessage(23, LogLevel.Information, "Realtime config created")]
    public static partial void RealtimeConfigCreated(this ILogger logger);
}
