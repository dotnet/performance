// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Threading.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_SpinLock
    {
        private SpinLock _spinLock = new SpinLock(enableThreadOwnerTracking: false);
        private SpinLock _acquiredSpinLock;

        [GlobalSetup(Target = nameof(TryEnter_Fail))]
        public void AcquireAcquiredSpinLock()
        {
            _acquiredSpinLock = new SpinLock(enableThreadOwnerTracking: false);
            bool lockTaken = false;
            _acquiredSpinLock.Enter(ref lockTaken);
        }

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

        [Benchmark]
        public bool TryEnter_Fail()
        {
            bool lockTaken = false;
            _acquiredSpinLock.TryEnter(0, ref lockTaken);
            return lockTaken;
        }
    }
}
