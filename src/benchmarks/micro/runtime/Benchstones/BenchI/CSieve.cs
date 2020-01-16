// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Based on Eratosthenes Sieve Prime Number Program in C, Byte Magazine, January 1983.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.BenchI)]
public class CSieve
{
    public const int Iterations = 200;

    const int Size = 8190;

    [Benchmark(Description = nameof(CSieve))]
    public bool Test() {
        bool[] flags = new bool[Size + 1];
        int count = 0;
        for (int iter = 1; iter <= Iterations; iter++)
        {
            count = 0;

            // Initially, assume all are prime
            for (int i = 0; i <= Size; i++)
            {
                flags[i] = true;
            }

            // Refine
            for (int i = 2; i <= Size; i++)
            {
                if (flags[i])
                {
                    // Found a prime
                    for (int k = i + i; k <= Size; k += i)
                    {
                        // Cancel its multiples
                        flags[k] = false;
                    }
                    count++;
                }
            }
        }

        return (count == 1027);
    }
}
}
