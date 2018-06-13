// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// The Adams-Moulton Predictor Corrector Method adapted from Conte and de Boor
// original source: adams_d.c

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace Benchstone.BenchF
{
[BenchmarkCategory(Categories.CoreCLR, Categories.Benchstones, Categories.BenchF)]
public class Adams
{
    static double g_xn, g_yn, g_dn, g_en;
    const double g_xn_base = 0.09999999E+01;
    const double g_yn_base = 0.71828180E+00;
    const double g_dn_base = 0.21287372E-08;
    const double g_en_base = 0.74505806E-08;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bench()
    {
        double[] f = new double[5];
        double xn, yn, dn, en, yxn, h, fnp, ynp, y0, x0;
        int i, k, n, nstep;

#if VERBOSE
        Console.WriteLine(" ADAMS-MOULTON METHOD ");
#endif // VERBOSE

        n = 4;
        h = 1.0 / 32.0;
        nstep = 32;
        y0 = 0.0;
        x0 = 0.0;
        xn = 0.0;
        yn = 0.0;
        dn = 0.0;
        en = 0.0;

        f[1] = x0 + y0;
#if VERBOSE
        Console.WriteLine("{0},  {1},  {2},  {3}", x0, y0, dn, en);
#endif // VERBOSE
        xn = x0;
        for (i = 2; i <= 4; i++)
        {
            k = i - 1;
            xn = xn + h;
            yn = Soln(xn);
            f[i] = xn + yn;
#if VERBOSE
            Console.WriteLine("{0},  {1},  {2},  {3},  {4}", k, xn, yn, dn, en);
#endif // VERBOSE
        }

        for (k = 4; k <= nstep; k++)
        {
            ynp = yn + (h / 24) * (55 * f[n] - 59 * f[n - 1] + 37 * f[n - 2] - 9 * f[n - 3]);
            xn = xn + h;
            fnp = xn + ynp;
            yn = yn + (h / 24) * (9 * fnp + 19 * f[n] - 5 * f[n - 1] + f[n - 2]);
            dn = (yn - ynp) / 14;
            f[n - 3] = f[n - 2];
            f[n - 2] = f[n - 1];
            f[n - 1] = f[n];
            f[n] = xn + yn;
            yxn = Soln(xn);
            en = yn - yxn;
#if VERBOSE
            Console.WriteLine("{0},  {1},  {2},  {3},  {4}", k, xn, yn, dn, en);
#endif // VERBOSE
        }

        // Escape calculated values:
        g_xn = xn;
        g_yn = yn;
        g_dn = dn;
        g_en = en;
    }

    private static double Soln(double x)
    {
        return (System.Math.Exp(x) - 1.0 - (x));
    }

    [Benchmark(Description = nameof(Adams))]
    public void Test() => Bench();
}
}
