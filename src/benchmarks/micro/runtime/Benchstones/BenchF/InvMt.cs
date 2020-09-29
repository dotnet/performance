// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// Solution of linear algebraic equations and matrix inversion.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchF
{
[BenchmarkCategory(Categories.Runtime, Categories.BenchF)]
public class InvMt
{
    public const int Iterations = 80;

    private const int MatSize = Iterations;

    private static T[][] AllocArray<T>(int n1, int n2)
    {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i)
        {
            a[i] = new T[n2];
        }
        return a;
    }

    [Benchmark(Description = nameof(InvMt))]
    public bool Test()
    {
        double[][] t = AllocArray<double>(MatSize + 1, (MatSize + 1) * 2);

        double det, detinv, ber, p;
        int n, i, j;

        n = MatSize;
        for (i = 1; i <= n; i++)
        {
            for (j = 1; j <= n; j++)
            {
                if (i == j)
                {
                    t[i][j] = 2.0001;
                    t[i][n + 1 + j] = 1.0;
                }
                else
                {
                    t[i][j] = 1.0001;
                    t[i][n + 1 + j] = 0.0;
                }
            }
            t[i][n + 1] = System.Math.Sqrt((float)i);
        }

        Inner(t, out det, ref n);

        for (i = 1; i <= n; i++)
        {
            for (j = 1; j <= n; j++)
            {
                p = t[i][j];
                t[i][j] = t[i][n + 1 + j];
                t[i][n + 1 + j] = p;
            }
        }

        Inner(t, out detinv, ref n);

        ber = 0.0;
        for (i = 1; i <= n; i++)
        {
            ber = ber + System.Math.Abs(System.Math.Sqrt((double)i) - t[i][n + 1]);
        }

        return true;
    }

    private static void Inner(double[][] t, out double det, ref int n)
    {
        double tik, tkk;

        det = 1.0;
        for (int k = 1; k <= n; k++)
        {
            tkk = t[k][k];
            det = det * tkk;

            for (int j = 1; j <= (2 * n + 1); j++)
            {
                t[k][j] = t[k][j] / tkk;
            }

            for (int i = 1; i <= n; i++)
            {
                if (i != k)
                {
                    tik = t[i][k];
                    for (int j = 1; j <= (2 * n + 1); j++)
                    {
                        t[i][j] = t[i][j] - t[k][j] * tik;
                    }
                }
            }
        }
    }
}
}
