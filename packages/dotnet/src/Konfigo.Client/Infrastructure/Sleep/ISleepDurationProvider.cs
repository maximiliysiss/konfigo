using System;

namespace Konfigo.Client.Infrastructure.Sleep;

internal interface ISleepDurationProvider
{
    TimeSpan GetSleepDelay(int attempt);
}
