using System;

namespace Konfigo.Application.Infrastructure.DateTime;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetNow() => DateTimeOffset.UtcNow;
}
