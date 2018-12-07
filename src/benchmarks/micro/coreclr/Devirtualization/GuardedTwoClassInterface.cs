// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

// Performance test for interface call dispatch with two
// possible target classes mixed in varying proportions.
//
// Note: for now we type the test input as B[] and not I[]
// so the simple guessing heuristic in the jit has a class
// to guess for.

namespace GuardedDevirtualizationTwoClassInterface
{

interface I
{
   int F();
}

public class B : I
{
    int I.F() => 33;
}

public class D : B, I
{
    int I.F() => 44;
}

public class TestInput
{
    public const int N = 1000;

    public TestInput(double pB)
    {
        _pB = pB;
        b = GetArray();
    }

    static Random r = new Random(42);

    double _pB;
    B[] b;

    public B[] Array => b;
    public override string ToString() => $"pB = {_pB:F2}";

    B[] GetArray()
    {
        B[] result = new B[N];
        for (int i = 0; i < N; i++)
        {
            double p = r.NextDouble();
            if (p > _pB)
            {
                result[i] = new D();
            }
            else
            {
                result[i] = new B();
            }
        }
        return result;
    }
}

[BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
public class Interface
{
    [Benchmark(OperationsPerInvoke=TestInput.N)]
    [ArgumentsSource(nameof(GetInput))]
    public long Call2(TestInput testInput)
    {
        long sum = 0;

        B[] input = testInput.Array;
        for (int i = 0; i < input.Length; i++)
        {
            sum += ((I)input[i]).F();
        }
        return sum;
    }

    static int S = 10;
    static double delta = 1.0 / (double) S;

    public static IEnumerable<TestInput> GetInput()
    {
        double pB = 0;

        for (int i = 0; i <= S; i++, pB += delta)
        {
            yield return new TestInput(pB);
        }
    }
}

}


