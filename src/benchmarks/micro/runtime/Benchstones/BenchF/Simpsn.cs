// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Integration by Simpson's rule adapted from Conte and de Boor

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchF
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.BenchF)]
public class Simpsn
{
    public const int Iterations = 90000;

    [Benchmark(Description = nameof(Simpsn))]
    public bool Test()
    {
        double a, b, x, s, c, h, hov2, half, t1;
        int idbg, n, nm1;

        s = 0;
        idbg = 0;
        if (idbg != 0)
        {
            System.Console.WriteLine("simpsons rule\n");
        }

        for (int j = 1; j <= Iterations; j++)
        {
            a = 0;
            b = 1;
            c = 4;
            n = 100;
            h = (b - a) / n;
            hov2 = h / System.Math.Sqrt(c);
            s = 0;
            t1 = a + hov2;
            half = F(t1);
            nm1 = n - 1;
            for (int i = 1; i <= nm1; i++)
            {
                x = a + i * h;
                s = s + F(x);
                t1 = x + hov2;
                half = half + F(t1);
                s = (h / 6) * (F(a) + 4 * half + 2 * s + F(b));
                if (idbg != 0)
                {
                    System.Console.WriteLine(" integral from a = {0} to b = {1} for n = {2} is {3}\n", a, b, n, s);
                }
            }
        }

        return true;
    }

    private static double F(double x)
    {
        return (System.Math.Exp((-(x)) * 2));
    }
}
}
