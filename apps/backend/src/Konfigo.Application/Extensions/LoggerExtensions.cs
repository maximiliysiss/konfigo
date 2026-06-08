using Konfigo.Domain.ValueType;
using Microsoft.Extensions.Logging;

namespace Konfigo.Application.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "Application service create started. Name: {Name}")]
    public static partial void LogApplicationServiceCreateStarted(this ILogger logger, string? name);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Application service created. ServiceId: {ServiceId}, Name: {Name}")]
    public static partial void LogApplicationServiceCreated(this ILogger logger, ServiceId serviceId, string? name);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "Application service update started. ServiceId: {ServiceId}, Name: {Name}")]
    public static partial void LogApplicationServiceUpdateStarted(this ILogger logger, ServiceId serviceId, string? name);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Application service updated. ServiceId: {ServiceId}, Name: {Name}")]
    public static partial void LogApplicationServiceUpdated(this ILogger logger, ServiceId serviceId, string? name);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug, Message = "Application service delete started. ServiceId: {ServiceId}")]
    public static partial void LogApplicationServiceDeleteStarted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Application service deleted. ServiceId: {ServiceId}, Name: {Name}")]
    public static partial void LogApplicationServiceDeleted(this ILogger logger, ServiceId serviceId, string? name);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Warning, Message = "Application service not found. ServiceId: {ServiceId}")]
    public static partial void LogApplicationServiceNotFound(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Debug, Message = "Application service member add started. ServiceId: {ServiceId}, UserId: {UserId}")]
    public static partial void LogApplicationServiceMemberAddStarted(this ILogger logger, ServiceId serviceId, UserId userId);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Application service member added. ServiceId: {ServiceId}, UserId: {UserId}")]
    public static partial void LogApplicationServiceMemberAdded(this ILogger logger, ServiceId serviceId, UserId userId);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Debug, Message = "Application service member add skipped. ServiceId: {ServiceId}, UserId: {UserId}")]
    public static partial void LogApplicationServiceMemberAddSkipped(this ILogger logger, ServiceId serviceId, UserId userId);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Debug, Message = "Application service member remove started. ServiceId: {ServiceId}, UserId: {UserId}")]
    public static partial void LogApplicationServiceMemberRemoveStarted(this ILogger logger, ServiceId serviceId, UserId userId);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Information, Message = "Application service member removed. ServiceId: {ServiceId}, UserId: {UserId}")]
    public static partial void LogApplicationServiceMemberRemoved(this ILogger logger, ServiceId serviceId, UserId userId);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Debug, Message = "Application service member remove skipped. ServiceId: {ServiceId}, UserId: {UserId}")]
    public static partial void LogApplicationServiceMemberRemoveSkipped(this ILogger logger, ServiceId serviceId, UserId userId);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Debug, Message = "Config entry create started. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogConfigEntryCreateStarted(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Information, Message = "Config entry created. ServiceId: {ServiceId}, VersionId: {VersionId}, EntryId: {EntryId}")]
    public static partial void LogConfigEntryCreated(this ILogger logger, ServiceId serviceId, VersionId versionId, EntryId entryId);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Debug, Message = "Config entry update started. ServiceId: {ServiceId}, VersionId: {VersionId}, EntryId: {EntryId}")]
    public static partial void LogConfigEntryUpdateStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, EntryId entryId);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Information, Message = "Config entry updated. ServiceId: {ServiceId}, VersionId: {VersionId}, EntryId: {EntryId}")]
    public static partial void LogConfigEntryUpdated(this ILogger logger, ServiceId serviceId, VersionId versionId, EntryId entryId);

    [LoggerMessage(EventId = 1104, Level = LogLevel.Debug, Message = "Config entry set started. ServiceId: {ServiceId}, VersionId: {VersionId}, RequestCount: {RequestCount}")]
    public static partial void LogConfigEntrySetStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, int requestCount);

    [LoggerMessage(EventId = 1105, Level = LogLevel.Information, Message = "Config entry set completed. ServiceId: {ServiceId}, VersionId: {VersionId}, AffectedCount: {AffectedCount}")]
    public static partial void LogConfigEntrySetCompleted(this ILogger logger, ServiceId serviceId, VersionId versionId, int affectedCount);

    [LoggerMessage(EventId = 1106, Level = LogLevel.Debug, Message = "Config entry delete started. ServiceId: {ServiceId}, VersionId: {VersionId}, EntryId: {EntryId}")]
    public static partial void LogConfigEntryDeleteStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, EntryId entryId);

    [LoggerMessage(EventId = 1107, Level = LogLevel.Information, Message = "Config entry deleted. ServiceId: {ServiceId}, VersionId: {VersionId}, EntryId: {EntryId}")]
    public static partial void LogConfigEntryDeleted(this ILogger logger, ServiceId serviceId, VersionId versionId, EntryId entryId);

    [LoggerMessage(EventId = 1108, Level = LogLevel.Warning, Message = "Config entry not found. ServiceId: {ServiceId}, VersionId: {VersionId}, EntryId: {EntryId}")]
    public static partial void LogConfigEntryNotFound(this ILogger logger, ServiceId serviceId, VersionId versionId, EntryId entryId);

    [LoggerMessage(EventId = 1109, Level = LogLevel.Warning, Message = "Config entries not found. ServiceId: {ServiceId}, VersionId: {VersionId}, RequestedCount: {RequestedCount}, FoundCount: {FoundCount}")]
    public static partial void LogConfigEntriesNotFound(this ILogger logger, ServiceId serviceId, VersionId versionId, int requestedCount, int foundCount);

    [LoggerMessage(EventId = 1200, Level = LogLevel.Debug, Message = "Config version create started. ServiceId: {ServiceId}, VersionLabel: {VersionLabel}")]
    public static partial void LogConfigVersionCreateStarted(this ILogger logger, ServiceId serviceId, string? versionLabel);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Config version created. ServiceId: {ServiceId}, VersionId: {VersionId}, VersionLabel: {VersionLabel}")]
    public static partial void LogConfigVersionCreated(this ILogger logger, ServiceId serviceId, VersionId versionId, string? versionLabel);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Debug, Message = "Config version update started. ServiceId: {ServiceId}, VersionId: {VersionId}, VersionLabel: {VersionLabel}")]
    public static partial void LogConfigVersionUpdateStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, string? versionLabel);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Information, Message = "Config version updated. ServiceId: {ServiceId}, VersionId: {VersionId}, VersionLabel: {VersionLabel}")]
    public static partial void LogConfigVersionUpdated(this ILogger logger, ServiceId serviceId, VersionId versionId, string? versionLabel);

    [LoggerMessage(EventId = 1204, Level = LogLevel.Debug, Message = "Config version generate started. ServiceId: {ServiceId}, VersionLabel: {VersionLabel}, EntryCount: {EntryCount}")]
    public static partial void LogConfigVersionGenerateStarted(this ILogger logger, ServiceId serviceId, string? versionLabel, int entryCount);

    [LoggerMessage(EventId = 1205, Level = LogLevel.Information, Message = "Config version generated. ServiceId: {ServiceId}, VersionId: {VersionId}, VersionLabel: {VersionLabel}, EntryCount: {EntryCount}")]
    public static partial void LogConfigVersionGenerated(this ILogger logger, ServiceId serviceId, VersionId versionId, string? versionLabel, int entryCount);

    [LoggerMessage(EventId = 1206, Level = LogLevel.Warning, Message = "Config version not found. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogConfigVersionNotFound(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 1207, Level = LogLevel.Warning, Message = "Config version already exists. ServiceId: {ServiceId}, VersionId: {VersionId}, VersionLabel: {VersionLabel}")]
    public static partial void LogConfigVersionAlreadyExists(this ILogger logger, ServiceId serviceId, VersionId versionId, string versionLabel);

    [LoggerMessage(EventId = 1300, Level = LogLevel.Warning, Message = "Access denied for service. ServiceId: {ServiceId}, UserId: {UserId}")]
    public static partial void LogAccessDenied(this ILogger logger, ServiceId serviceId, UserId? userId = null);

    [LoggerMessage(EventId = 1400, Level = LogLevel.Debug, Message = "Entry create audit completed. ServiceId: {ServiceId}")]
    public static partial void LogEntryCreateAuditCompleted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 1401, Level = LogLevel.Debug, Message = "Entry update audit completed. ServiceId: {ServiceId}")]
    public static partial void LogEntryUpdateAuditCompleted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 1402, Level = LogLevel.Debug, Message = "Entry set audit completed. ServiceId: {ServiceId}, AuditLogCount: {AuditLogCount}")]
    public static partial void LogEntrySetAuditCompleted(this ILogger logger, ServiceId serviceId, int auditLogCount);

    [LoggerMessage(EventId = 1403, Level = LogLevel.Debug, Message = "Entry delete audit completed. ServiceId: {ServiceId}")]
    public static partial void LogEntryDeleteAuditCompleted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 1404, Level = LogLevel.Debug, Message = "Version create audit completed. ServiceId: {ServiceId}")]
    public static partial void LogVersionCreateAuditCompleted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 1405, Level = LogLevel.Debug, Message = "Version update audit completed. ServiceId: {ServiceId}")]
    public static partial void LogVersionUpdateAuditCompleted(this ILogger logger, ServiceId serviceId);

    [LoggerMessage(EventId = 1406, Level = LogLevel.Debug, Message = "Version generate audit completed. ServiceId: {ServiceId}, AuditLogCount: {AuditLogCount}")]
    public static partial void LogVersionGenerateAuditCompleted(this ILogger logger, ServiceId serviceId, int auditLogCount);

    [LoggerMessage(EventId = 1500, Level = LogLevel.Debug, Message = "Notification skipped empty entry batch. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogNotificationSkippedEmptyEntryBatch(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 1501, Level = LogLevel.Debug, Message = "Notification started. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogNotificationStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 1502, Level = LogLevel.Information, Message = "Notification completed. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogNotificationCompleted(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 1503, Level = LogLevel.Debug, Message = "Outbox send skipped empty request. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogOutboxSendSkippedEmptyRequest(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 1504, Level = LogLevel.Debug, Message = "Outbox send started. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogOutboxSendStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 1505, Level = LogLevel.Information, Message = "Outbox sent. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogOutboxSent(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 1506, Level = LogLevel.Debug, Message = "Outbox execute started. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogOutboxExecuteStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 1507, Level = LogLevel.Information, Message = "Outbox executed. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogOutboxExecuted(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 1600, Level = LogLevel.Debug, Message = "Subscriber create started. ServiceId: {ServiceId}, VersionId: {VersionId}, SubscriberCount: {SubscriberCount}")]
    public static partial void LogSubscriberCreateStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, int subscriberCount);

    [LoggerMessage(EventId = 1601, Level = LogLevel.Warning, Message = "Subscriber create duplicate. ServiceId: {ServiceId}, VersionId: {VersionId}, SubscriberCount: {SubscriberCount}")]
    public static partial void LogSubscriberCreateDuplicate(this ILogger logger, ServiceId serviceId, VersionId versionId, int subscriberCount);

    [LoggerMessage(EventId = 1602, Level = LogLevel.Information, Message = "Subscriber created. ServiceId: {ServiceId}, VersionId: {VersionId}, SubscriberCount: {SubscriberCount}")]
    public static partial void LogSubscriberCreated(this ILogger logger, ServiceId serviceId, VersionId versionId, int subscriberCount);

    [LoggerMessage(EventId = 1603, Level = LogLevel.Debug, Message = "Subscriber publish skipped because there are no subscribers. ServiceId: {ServiceId}, VersionId: {VersionId}, EventCount: {EventCount}")]
    public static partial void LogSubscriberPublishNoSubscribers(this ILogger logger, ServiceId serviceId, VersionId versionId, int eventCount);

    [LoggerMessage(EventId = 1604, Level = LogLevel.Debug, Message = "Subscriber publish started. ServiceId: {ServiceId}, VersionId: {VersionId}, SubscriberCount: {SubscriberCount}, EventCount: {EventCount}")]
    public static partial void LogSubscriberPublishStarted(this ILogger logger, ServiceId serviceId, VersionId versionId, int subscriberCount, int eventCount);

    [LoggerMessage(EventId = 1605, Level = LogLevel.Information, Message = "Subscriber published. ServiceId: {ServiceId}, VersionId: {VersionId}, SubscriberCount: {SubscriberCount}, EventCount: {EventCount}")]
    public static partial void LogSubscriberPublished(this ILogger logger, ServiceId serviceId, VersionId versionId, int subscriberCount, int eventCount);

    [LoggerMessage(EventId = 1606, Level = LogLevel.Error, Message = "Subscriber delete failed because there are no subscribers. ServiceId: {ServiceId}, VersionId: {VersionId}")]
    public static partial void LogSubscriberDeleteNoSubscribers(this ILogger logger, ServiceId serviceId, VersionId versionId);

    [LoggerMessage(EventId = 1607, Level = LogLevel.Warning, Message = "Subscriber delete failed. ServiceId: {ServiceId}, VersionId: {VersionId}, SubscriberCount: {SubscriberCount}")]
    public static partial void LogSubscriberDeleteFailed(this ILogger logger, ServiceId serviceId, VersionId versionId, int subscriberCount);

    [LoggerMessage(EventId = 1608, Level = LogLevel.Information, Message = "Subscriber deleted. ServiceId: {ServiceId}, VersionId: {VersionId}, SubscriberCount: {SubscriberCount}")]
    public static partial void LogSubscriberDeleted(this ILogger logger, ServiceId serviceId, VersionId versionId, int subscriberCount);
}
