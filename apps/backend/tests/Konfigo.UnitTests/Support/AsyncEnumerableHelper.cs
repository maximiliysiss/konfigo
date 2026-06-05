using System.Collections.Generic;
using System.Threading.Tasks;

namespace Konfigo.UnitTests.Support;

internal static class AsyncEnumerableHelper
{
    public static async IAsyncEnumerable<T> From<T>(T item)
    {
        yield return item;
        await Task.CompletedTask;
    }

    public static async IAsyncEnumerable<T> FromMany<T>(params T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }

#pragma warning disable CS1998
    public static async IAsyncEnumerable<T> Empty<T>()
    {
        yield break;
    }
#pragma warning restore CS1998
}
