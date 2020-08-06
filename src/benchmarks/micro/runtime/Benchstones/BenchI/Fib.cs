// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.BenchI)]
public class Fib
{
    const int Number = 24;

    static int Fibonacci(int x) {
        if (x > 2) {
            return (Fibonacci(x - 1) + Fibonacci(x - 2));
        }
        else {
            return 1;
        }
    }

    [Benchmark(Description = nameof(Fib))]
    public bool Test() {
        int fib = Fibonacci(Number);
        return (fib == 46368);
    }
}
}
