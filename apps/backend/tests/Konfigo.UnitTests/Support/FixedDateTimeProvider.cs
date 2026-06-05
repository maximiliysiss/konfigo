using System;
using Konfigo.Application.Infrastructure.DateTime;

namespace Konfigo.UnitTests.Support;

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset GetNow() => TestFakes.Now;
}
