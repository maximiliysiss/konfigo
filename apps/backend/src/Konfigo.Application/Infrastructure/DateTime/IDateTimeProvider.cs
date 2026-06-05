using System;

namespace Konfigo.Application.Infrastructure.DateTime;

public interface IDateTimeProvider
{
    DateTimeOffset GetNow();
}
