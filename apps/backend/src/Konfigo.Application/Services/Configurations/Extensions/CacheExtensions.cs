using Konfigo.Domain.ValueType;

namespace Konfigo.Application.Services.Configurations.Extensions;

internal static class CacheExtensions
{
    public static string AsKey(this (ServiceId ServiceId, VersionId VersionId) key) => $"{key.ServiceId}_{key.VersionId}::entry";
    public static string AsKey(this (ServiceId ServiceId, string VersionLabel) key) => $"{key.ServiceId}::{key.VersionLabel}::entry";
}
