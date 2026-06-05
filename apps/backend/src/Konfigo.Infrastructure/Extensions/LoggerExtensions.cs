using Microsoft.Extensions.Logging;

namespace Konfigo.Infrastructure.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Running migrations...")]
    public static partial void LogRunningMigrations(this ILogger logger);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Migrations completed successfully.")]
    public static partial void LogMigrationsCompleted(this ILogger logger);
}
