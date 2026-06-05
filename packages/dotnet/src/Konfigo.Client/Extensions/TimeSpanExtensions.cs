using System;
using System.Security.Cryptography;

namespace Konfigo.Client.Extensions;

internal static class TimeSpanExtensions
{
    public static TimeSpan Jitter(this TimeSpan timeSpan, int min = 0, int max = 100)
    {
        var milliseconds = RandomNumberGenerator.GetInt32(min, max);
        return timeSpan + TimeSpan.FromMilliseconds(milliseconds);
    }
}
