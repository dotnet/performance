// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_SpinLock
    {
        SpinLock _spinLock = new SpinLock();

        [Benchmark]
        public bool EnterExit()
        {
            SpinLock spinLock = _spinLock;

            bool lockTaken = false;

            spinLock.Enter(ref lockTaken);
            spinLock.Exit();

            return lockTaken;
        }

        [Benchmark]
        public bool TryEnterExit()
        {
            SpinLock spinLock = _spinLock;

            bool lockTaken = false;

            spinLock.TryEnter(0, ref lockTaken);
            spinLock.Exit();

            return lockTaken;
        }
    }
}