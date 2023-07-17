// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.MDBenchF
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.MDBenchF)]
public class MDInProd
{
    public const int Iterations = 70;

    private const int RowSize = 10 * Iterations;

    private static int s_seed;

    [Benchmark(Description = nameof(MDInProd))]
    [MemoryRandomization]
    public bool Test()
    {
        double[,] rma = new double[RowSize, RowSize];
        double[,] rmb = new double[RowSize, RowSize];
        double[,] rmr = new double[RowSize, RowSize];

        double sum;

        Inner(rma, rmb, rmr);

        for (int i = 1; i < RowSize; i++)
        {
            for (int j = 1; j < RowSize; j++)
            {
                sum = 0;
                for (int k = 1; k < RowSize; k++)
                {
                    sum = sum + rma[i,k] * rmb[k,j];
                }
                if (rmr[i,j] != sum)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void InitRand()
    {
        s_seed = 7774755;
    }

    private static int Rand()
    {
        s_seed = (s_seed * 77 + 13218009) % 3687091;
        return s_seed;
    }

    private static void InitMatrix(double[,] m)
    {
        for (int i = 1; i < RowSize; i++)
        {
            for (int j = 1; j < RowSize; j++)
            {
                m[i,j] = (Rand() % 120 - 60) / 3;
            }
        }
    }

    private static void InnerProduct(out double result, double[,] a, double[,] b, int row, int col)
    {
        result = 0.0;
        for (int i = 1; i < RowSize; i++)
        {
            result = result + a[row,i] * b[i,col];
        }
    }

    private static void Inner(double[,] rma, double[,] rmb, double[,] rmr)
    {
        InitRand();
        InitMatrix(rma);
        InitMatrix(rmb);
        for (int i = 1; i < RowSize; i++)
        {
            for (int j = 1; j < RowSize; j++)
            {
                InnerProduct(out rmr[i,j], rma, rmb, i, j);
            }
        }
    }
}
}
