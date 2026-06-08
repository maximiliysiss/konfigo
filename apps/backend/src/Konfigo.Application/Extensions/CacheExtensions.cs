using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Extensions;

internal static class CacheExtensions
{
    public static string AsKey(this (ServiceId ServiceId, VersionId VersionId) key) => $"{key.ServiceId}::{key.VersionId}::entry";
    public static string AsKey(this (ServiceId ServiceId, string VersionLabel) key) => $"{key.ServiceId}::{key.VersionLabel}::entry::clear";
    public static string AsKey(this ServiceId serviceId) => $"{serviceId}::service";
    public static string AsKey(this VersionId versionId) => $"{versionId}::version";
    public static string AsKey(this (ServiceId ServiceId, UserId UserId) key) => $"{key.ServiceId}::{key.UserId}::user";
}
