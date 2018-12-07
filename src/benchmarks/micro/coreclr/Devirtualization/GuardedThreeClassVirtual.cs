// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

// Performance test for virtual call dispatch with three
// possible target classes mixed in varying proportions.

namespace GuardedDevirtualizationThreeClass
{

public class B
{
    public virtual int F() => 33;
}

public class D : B
{
    public override int F() => 44;
}

public class E : B
{
    public override int F() => 55;
}

public class TestInput
{
    public const int N = 1000;

    public TestInput(double pB, double pD)
    {
        _pB = pB;
        _pD = pD;
        b = GetArray();
    }

    static Random r = new Random(42);

    double _pB;
    double _pD;
    B[] b;

    public B[] Array => b;
    public override string ToString() => $"pB={_pB:F2} pD={_pD:F2}";

    B[] GetArray()
    {
        B[] result = new B[N];

        for (int i = 0; i < N; i++)
        {
            double p = r.NextDouble();

            if (p <= _pB)
            {
                result[i] = new B();
            }
            else if (p <= _pB + _pD)
            {
                result[i] = new D();
            }
            else
            {
                result[i] = new E();
            }
        }
        return result;
    }
}

[BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
public class Virtual
{
    [Benchmark(OperationsPerInvoke=TestInput.N)]
    [ArgumentsSource(nameof(GetInput))]
    public long Call3(TestInput testInput)
    {
        long sum = 0;
        B[] input = testInput.Array;
        for (int i = 0; i < input.Length; i++)
        {
            sum += input[i].F();
        }
        return sum;
    }

    static int S = 3;
    static double delta = 1.0 / (double) S;

    public static IEnumerable<TestInput> GetInput()
    {
        double pB = 0;

        for (int i = 0; i <= S; i++, pB += delta)
        {
            double pD = 0;

            for (int j = 0; j <= S - i; j++, pD += delta)
            {
                yield return new TestInput(pB, pD);
            }
        }
    }
}

}


