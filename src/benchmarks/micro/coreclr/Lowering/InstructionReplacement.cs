// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Lowering
{
    [BenchmarkCategory(Categories.CoreCLR)]
    public unsafe class InstructionReplacement
    {
        [Benchmark(OperationsPerInvoke = 10_000_000)]
        public int TESTtoBT()
        {
            int y = 0, x = 0;

            while (x++ < 10_000_000)
            {
                if ((x & (1 << y)) == 0)
                {
                    y++;
                }
            }

            return y;
        }
    }
}
