// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Integration by corrected trapezoid rule adapted from Conte and de Boor

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchF
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.BenchF)]
public class Trap
{
    public const int Iterations = 240000;

    [Benchmark(Description = nameof(Trap))]
    public bool Test()
    {
        int nm1, idbg;
        double t2, cortrp, trap, a, b, h;
        trap = 0.0;
        cortrp = 0.0;

        idbg = 0;
        for (int j = 1; j <= Iterations; j++)
        {
            a = 0;
            b = 1;
            if (idbg != 0)
            {
                System.Console.WriteLine("trapazoid sum    corr.trap sum \n");
            }

            for (int n = 10; n <= 15; n++)
            {
                h = (b - a) / n;
                nm1 = n - 1;
                trap = (F(a) + F(b)) / 2;
                for (int i = 1; i <= nm1; i++)
                {
                    t2 = a + i * h;
                    trap = trap + F(t2);
                }
                trap = trap * h;
                cortrp = trap + h * h * (FPrime(a) - FPrime(b)) / 12;
                if (idbg != 0)
                {
                    System.Console.WriteLine("{0}, {1}, {2}\n", n, trap, cortrp);
                }
            }
        }

        return true;
    }

    private static double F(double x)
    {
        return (System.Math.Exp(-(x) * (x)));
    }

    private static double FPrime(double x)
    {
        return ((-2) * (x) * (F(x)));
    }
}
}
