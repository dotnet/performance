// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Lock
    {
        private const int IterationCount = 2_000_000;

        ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        [Benchmark]
        public void ReaderWriterLockSlimPerf()
        {
            ReaderWriterLockSlim rwLock = _rwLock;

            for (int i = 0; i < IterationCount; i++)
            {
                rwLock.EnterReadLock();
                rwLock.ExitReadLock();
            }
        }
    }
}