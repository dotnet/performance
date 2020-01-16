// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Threading.Tasks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_CancellationToken
    {
        private readonly CancellationToken _token = new CancellationTokenSource().Token;

        [Benchmark]
        public void RegisterAndUnregister_Serial() => _token.Register(() => { }).Dispose();

        [Benchmark(OperationsPerInvoke = 1_000_000)]
        public void RegisterAndUnregister_Parallel() =>
            Parallel.For(0, 1_000_000, i => _token.Register(() => { }).Dispose());

        [Benchmark]
        public void Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Token.Register(() => { });
            cts.Cancel();
        }

        private CancellationToken _token1 = new CancellationTokenSource().Token;
        private CancellationToken _token2 = new CancellationTokenSource().Token;
        private CancellationToken[] _tokens = new[] { new CancellationTokenSource().Token, new CancellationTokenSource().Token, new CancellationTokenSource().Token };

        [Benchmark]
        public void CreateLinkedTokenSource1() =>
            CancellationTokenSource.CreateLinkedTokenSource(_token1, default).Dispose();

        [Benchmark]
        public void CreateLinkedTokenSource2() =>
            CancellationTokenSource.CreateLinkedTokenSource(_token1, _token2).Dispose();

        [Benchmark]
        public void CreateLinkedTokenSource3() =>
            CancellationTokenSource.CreateLinkedTokenSource(_tokens).Dispose();

        [Benchmark]
        public void CancelAfter()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(int.MaxValue - 1);
            }
        }
    }
}
