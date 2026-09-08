// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET10_0_OR_GREATER
using System.Collections.Generic;

namespace System.Linq;

// System.Linq.AsyncEnumerable ships in the shared framework from .NET 10. This project targets net8.0 (the lowest TFM the perf pipelines exercise),
// so the member it needs is polyfilled here rather than taking the System.Linq.AsyncEnumerable package. This mirrors BenchmarkDotNet's own polyfill.
internal static class AsyncEnumerable
{
    public static IAsyncEnumerable<TSource> ToAsyncEnumerable<TSource>(this IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return FromIterator(source);

        static async IAsyncEnumerable<TSource> FromIterator(IEnumerable<TSource> source)
        {
            foreach (TSource element in source)
            {
                yield return element;
            }
        }
    }
}
#endif
