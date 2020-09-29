// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.BenchI)]
public class Midpoint
{
    public const int Iterations = 70000;

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    static int Inner(ref int x, ref int y, ref int z) {
        int mid;

        if (x < y) {
            if (y < z) {
                mid = y;
            }
            else {
                if (x < z) {
                    mid = z;
                }
                else {
                    mid = x;
                }
            }
        }
        else {
            if (x < z) {
                mid = x;
            }
            else {
                if (y < z) {
                    mid = z;
                }
                else {
                    mid = y;
                }
            }
        }

        return (mid);
    }

    [Benchmark(Description = nameof(Midpoint))]
    public bool Test() {
        int[][] a = AllocArray<int>(2001, 4);
        int[] mid = new int[2001];
        int j = 99999;

        for (int i = 1; i <= 2000; i++) {
            a[i][1] = j & 32767;
            a[i][2] = (j + 11111) & 32767;
            a[i][3] = (j + 22222) & 32767;
            j = j + 33333;
        }

        for (int k = 1; k <= Iterations; k++) {
            for (int l = 1; l <= 2000; l++) {
                mid[l] = Inner(ref a[l][1], ref a[l][2], ref a[l][3]);
            }
        }

        return (mid[2000] == 17018);
    }
}
}
