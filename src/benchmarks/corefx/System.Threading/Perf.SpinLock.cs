// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Threading.Tests
{
    public class Perf_SpinLock
    {
        private const int IterationCount = 1_000_000;

        SpinLock _spinLock = new SpinLock();

        [Benchmark]
        public void EnterExit()
        {
            SpinLock spinLock = _spinLock;

            for (int i = 0; i < IterationCount; i++)
            {
                bool lockTaken = false;

                spinLock.Enter(ref lockTaken);
                spinLock.Exit();
            }
        }

        [Benchmark]
        public void TryEnterExit()
        {
            SpinLock spinLock = _spinLock;

            for (int i = 0; i < IterationCount; i++)
            {
                bool lockTaken = false;

                spinLock.TryEnter(0, ref lockTaken);
                spinLock.Exit();
            }
        }
    }
}