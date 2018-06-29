// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Threading.Tests
{
    public class Perf_Volatile
    {
        private const int IterationCount = 100_000_000;

        [Benchmark]
        public void Read_double()
        {
            double location = 0;

            for (int i = 0; i < IterationCount; i++)
            {
                Volatile.Read(ref location);
            }
        }

        [Benchmark]
        public void Write_double()
        {
            double location = 0;
            double newValue = 1;

            for (int i = 0; i < IterationCount; i++)
            {
                Volatile.Write(ref location, newValue);
            }
        }
    }
}