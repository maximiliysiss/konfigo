using System;
using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading;

namespace Konfigo.Application.Extensions;

internal static class DistributedLockProviderExtensions
{
    public static async Task<IDistributedSynchronizationHandle?> TryAcquireOrThrowAsync(
        this IDistributedLockProvider lockProvider,
        string key,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return await lockProvider.TryAcquireLockAsync(key, timeout, cancellationToken) ??
               throw new InvalidOperationException($"Concurrent access to {key} is not allowed.");
    }
}
