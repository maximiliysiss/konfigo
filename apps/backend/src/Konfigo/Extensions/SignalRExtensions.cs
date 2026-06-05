using Konfigo.Domain.ValueType;

namespace Konfigo.Extensions;

internal static class SignalRExtensions
{
    public static string AsSignalKey(this (ServiceId ServiceId, VersionId VersionId) key) => $"{key.ServiceId}_{key.VersionId}::signalr";
}
