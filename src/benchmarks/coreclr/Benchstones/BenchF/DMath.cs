// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using System;
using Benchmarks;

namespace Benchstone.BenchF
{
[BenchmarkCategory(Categories.CoreCLR, Categories.Benchstones, Categories.BenchF)]
public class DMath
{
    public const int Iterations = 100000;

    private const double Deg2Rad = 57.29577951;
    private static volatile object s_volatileObject;

    private static void Escape(object obj)
    {
        s_volatileObject = obj;
    }

    private static double Fact(double n)
    {
        double res;
        res = 1.0;
        while (n > 0.0)
        {
            res *= n;
            n -= 1.0;
        }

        return res;
    }

    private static double Power(double n, double p)
    {
        double res;
        res = 1.0;
        while (p > 0.0)
        {
            res *= n;
            p -= 1.0;
        }

        return res;
    }

    [Benchmark(Description = nameof(DMath))]
    [Arguments(Iterations)]
    public bool Test(int loop)
    {
        double[] sines = new double[91];
        double angle, radians, sine, worksine, temp, k;
        double diff;

        for (int iter = 1; iter <= loop; iter++)
        {
            for (angle = 0.0; angle <= 90.0; angle += 1.0)
            {
                radians = angle / Deg2Rad;
                k = 0.0;
                worksine = 0.0;
                do
                {
                    sine = worksine;
                    temp = (2.0 * k) + 1.0;
                    worksine += (Power(-1.0, k) / Fact(temp)) * Power(radians, temp);
                    k += 1.0;
                    diff = Math.Abs(sine - worksine);
                } while (diff > 1E-8);

                sines[(int)angle] = worksine;
            }
        }

        // Escape sines array so that its elements appear live-out
        Escape(sines);

        return true;
    }
}
}

