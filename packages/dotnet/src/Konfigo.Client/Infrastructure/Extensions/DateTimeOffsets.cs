using System;
using System.Runtime.CompilerServices;

namespace Konfigo.Client.Infrastructure.Extensions;

internal static class DateTimeOffsets
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
