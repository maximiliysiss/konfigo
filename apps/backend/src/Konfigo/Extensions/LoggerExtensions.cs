using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging;

namespace Konfigo.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug, Message = "gRPC get config started. ServiceId: {ServiceId}, VersionLabel: {VersionLabel}")]
    public static partial void LogGrpcGetConfigStarted(this ILogger logger, ServiceId serviceId, string? versionLabel);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "gRPC get config completed with missing version. ServiceId: {ServiceId}, VersionLabel: {VersionLabel}")]
    public static partial void LogGrpcGetConfigMissingVersion(this ILogger logger, ServiceId serviceId, string? versionLabel);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Debug, Message = "gRPC get config completed. ServiceId: {ServiceId}, VersionId: {VersionId}, EntryCount: {EntryCount}")]
    public static partial void LogGrpcGetConfigCompleted(this ILogger logger, ServiceId serviceId, VersionId versionId, int entryCount);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Debug, Message = "gRPC version existence checked. ServiceId: {ServiceId}, VersionLabel: {VersionLabel}, VersionId: {VersionId}")]
    public static partial void LogGrpcVersionExistenceChecked(this ILogger logger, ServiceId serviceId, string? versionLabel, VersionId? versionId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Debug, Message = "gRPC subscription started. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogGrpcSubscriptionStarted(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "gRPC subscription backfill sent. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogGrpcSubscriptionBackfillSent(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Debug, Message = "gRPC subscription event sent. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogGrpcSubscriptionEventSent(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Debug, Message = "gRPC config version generate started. ServiceId: {ServiceId}, VersionLabel: {VersionLabel}, EntryCount: {EntryCount}")]
    public static partial void LogConfigVersionGenerateStarted(this ILogger logger, ServiceId serviceId, string? versionLabel, int entryCount);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "gRPC config version generated. ServiceId: {ServiceId}, VersionId: {VersionId}, VersionLabel: {VersionLabel}, EntryCount: {EntryCount}")]
    public static partial void LogConfigVersionGenerated(this ILogger logger, ServiceId serviceId, VersionId versionId, string? versionLabel, int entryCount);

    [LoggerMessage(EventId = 2100, Level = LogLevel.Debug, Message = "Audit log search started. ServiceId: {ServiceId}, PageSize: {PageSize}")]
    public static partial void LogAuditLogSearchStarted(this ILogger logger, ServiceId serviceId, int pageSize);

    [LoggerMessage(EventId = 2101, Level = LogLevel.Debug, Message = "Audit log search completed. ServiceId: {ServiceId}, PageSize: {PageSize}, ResultCount: {ResultCount}")]
    public static partial void LogAuditLogSearchCompleted(this ILogger logger, ServiceId serviceId, int pageSize, int resultCount);

    [LoggerMessage(EventId = 2200, Level = LogLevel.Debug, Message = "Authentication login challenge started. ReturnUrl: {ReturnUrl}, SafeReturnUrl: {SafeReturnUrl}")]
    public static partial void LogAuthenticationLoginChallengeStarted(this ILogger logger, string? returnUrl, string safeReturnUrl);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Information, Message = "Authentication logout started. ReturnUrl: {ReturnUrl}, SafeReturnUrl: {SafeReturnUrl}")]
    public static partial void LogAuthenticationLogoutStarted(this ILogger logger, string? returnUrl, string safeReturnUrl);

    [LoggerMessage(EventId = 2202, Level = LogLevel.Debug, Message = "Current user request returned no content because identity is not authenticated.")]
    public static partial void LogCurrentUserUnauthenticated(this ILogger logger);

    [LoggerMessage(EventId = 2203, Level = LogLevel.Debug, Message = "Current user request completed. UserId: {UserId}, RoleCount: {RoleCount}")]
    public static partial void LogCurrentUserCompleted(this ILogger logger, string? userId, int roleCount);

    [LoggerMessage(EventId = 2300, Level = LogLevel.Debug, Message = "Service search skipped because user has no allowed services.")]
    public static partial void LogServiceSearchSkippedNoAllowedServices(this ILogger logger);

    [LoggerMessage(EventId = 2301, Level = LogLevel.Debug, Message = "Application service search completed. Name: {Name}, PageSize: {PageSize}, ResultCount: {ResultCount}")]
    public static partial void LogApplicationServiceSearchCompleted(this ILogger logger, string? name, int pageSize, int resultCount);

    [LoggerMessage(EventId = 2302, Level = LogLevel.Debug, Message = "Application service get by id started. ServiceId: {ServiceId}")]
    public static partial void LogApplicationServiceGetByIdStarted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 2303, Level = LogLevel.Debug, Message = "Application service get by id completed. ServiceId: {ServiceId}, Name: {Name}")]
    public static partial void LogApplicationServiceGetByIdCompleted(this ILogger logger, ServiceId serviceId, string? name);

    [LoggerMessage(EventId = 2304, Level = LogLevel.Warning, Message = "Application service not found. ServiceId: {ServiceId}")]
    public static partial void LogApplicationServiceNotFound(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 2305, Level = LogLevel.Warning, Message = "Access denied for service. ServiceId: {ServiceId}")]
    public static partial void LogAccessDenied(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 2500, Level = LogLevel.Debug, Message = "Config entry search started. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogConfigEntrySearchStarted(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 2501, Level = LogLevel.Debug, Message = "Config entry search completed. ServiceId: {ServiceId}, VersionId: {VersionId}, ResultCount: {ResultCount}")]
    public static partial void LogConfigEntrySearchCompleted(this ILogger logger, ServiceId serviceId, VersionId versionId, int resultCount);

    [LoggerMessage(EventId = 2600, Level = LogLevel.Debug, Message = "Config version search started. ServiceId: {ServiceId}")]
    public static partial void LogConfigVersionSearchStarted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 2601, Level = LogLevel.Debug, Message = "Config version search completed. ServiceId: {ServiceId}, VersionCount: {VersionCount}")]
    public static partial void LogConfigVersionSearchCompleted(this ILogger logger, ServiceId serviceId, int versionCount);

    [LoggerMessage(EventId = 2602, Level = LogLevel.Debug, Message = "Config version get by id started. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogConfigVersionGetByIdStarted(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 2603, Level = LogLevel.Debug, Message = "Config version get by id completed. ServiceId: {ServiceId}, VersionId: {VersionId}, VersionLabel: {VersionLabel}")]
    public static partial void LogConfigVersionGetByIdCompleted(this ILogger logger, ServiceId serviceId, VersionId versionId, string? versionLabel);

    [LoggerMessage(EventId = 2604, Level = LogLevel.Warning, Message = "Config version not found. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogConfigVersionNotFound(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 2400, Level = LogLevel.Debug, Message = "Service access check started. ServiceId: {ServiceId}")]
    public static partial void LogServiceAccessCheckStarted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 2401, Level = LogLevel.Debug, Message = "Service access check completed. ServiceId: {ServiceId}, ServiceName: {ServiceName}")]
    public static partial void LogServiceAccessCheckCompleted(this ILogger logger, ServiceId serviceId, string serviceName);
}
