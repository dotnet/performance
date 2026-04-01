// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Extensions
{
    /// <summary>
    /// Wraps a synchronous <see cref="IEnumerable{T}"/> as <see cref="IAsyncEnumerable{T}"/>
    /// without requiring System.Linq.AsyncEnumerable or Microsoft.Bcl.AsyncInterfaces packages
    /// at runtime. This avoids package pruning issues on .NET 10+ where those polyfill packages
    /// are removed but netstandard2.0 libraries still reference them.
    /// </summary>
    internal static class SyncAsyncEnumerable
    {
        public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> source)
            => new Wrapper<T>(source);

        private sealed class Wrapper<T> : IAsyncEnumerable<T>
        {
            private readonly IEnumerable<T> _source;
            public Wrapper(IEnumerable<T> source) => _source = source;
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new Enumerator<T>(_source.GetEnumerator());
        }

        private sealed class Enumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;
            public Enumerator(IEnumerator<T> inner) => _inner = inner;
            public T Current => _inner.Current;
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
            public ValueTask DisposeAsync()
            {
                _inner.Dispose();
                return default;
            }
        }
    }
}
