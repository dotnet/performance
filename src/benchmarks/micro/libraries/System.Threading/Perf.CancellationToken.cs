// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Threading.Tasks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_CancellationToken
    {
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private readonly CancellationToken _token = new CancellationTokenSource().Token;
        private CancellationToken _token1 = new CancellationTokenSource().Token;
        private CancellationToken _token2 = new CancellationTokenSource().Token;
        private CancellationToken[] _tokens = new[] { new CancellationTokenSource().Token, new CancellationTokenSource().Token, new CancellationTokenSource().Token };

        [Benchmark]
        public void RegisterAndUnregister_Serial() => _token.Register(() => { }).Dispose();

        [Benchmark]
        public void Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Token.Register(() => { });
            cts.Cancel();
        }

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
        [MemoryRandomization]
        public void CancelAfter()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(int.MaxValue - 1);
            }
        }

        [Benchmark]
        public void CreateTokenDispose()
        {
            using (var cts = new CancellationTokenSource())
                _ = cts.Token;
        }

        [Benchmark]
        public void CreateRegisterDispose()
        {
            using (var cts = new CancellationTokenSource())
                cts.Token.Register(s => { }, null).Dispose();
        }

        [Benchmark(OperationsPerInvoke = 1_000_000)]
        public void CreateManyRegisterDispose()
        {
            using (var cts = new CancellationTokenSource())
            {
                CancellationToken ct = cts.Token;
                for (int i = 0; i < 1_000_000; i++)
                    ct.Register(s => { }, null).Dispose();
            }
        }

        [Benchmark(OperationsPerInvoke = 1_000_000)]
        public void CreateManyRegisterMultipleDispose()
        {
            using (var cts = new CancellationTokenSource())
            {
                CancellationToken ct = cts.Token;
                for (int i = 0; i < 1_000_000; i++)
                {
                    var ctr1 = ct.Register(s => { }, null);
                    var ctr2 = ct.Register(s => { }, null);
                    var ctr3 = ct.Register(s => { }, null);
                    var ctr4 = ct.Register(s => { }, null);
                    var ctr5 = ct.Register(s => { }, null);
                    ctr5.Dispose();
                    ctr4.Dispose();
                    ctr3.Dispose();
                    ctr2.Dispose();
                    ctr1.Dispose();
                }
            }
        }
    }
}
