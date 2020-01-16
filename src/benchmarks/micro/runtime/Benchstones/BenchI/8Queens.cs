// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.BenchI)]
public class EightQueens
{
    static int[] m_c = new int[15];
    static int[] m_x = new int[9];

    static void TryMe(int i, ref int q, int[] a, int[] b)
    {
        int j = 0;
        q = 0;
        while ((q == 0) && (j != 8)) {
            j = j + 1;
            q = 0;
            if ((b[j] == 1) && (a[i + j] == 1) && (m_c[i - j + 7] == 1)) {
                m_x[i] = j;
                b[j] = 0;
                a[i + j] = 0;
                m_c[i - j + 7] = 0;
                if (i < 8) {
                    TryMe(i + 1, ref q, a, b);
                    if (q == 0) {
                        b[j] = 1;
                        a[i + j] = 1;
                        m_c[i - j + 7] = 1;
                    }
                }
                else {
                    q = 1;
                }
            }
        }
    }

    [Benchmark(Description = nameof(EightQueens))]
    public bool Test() {
        int[] a = new int[9];
        int[] b = new int[17];
        int q = 0;
        int i = 0;
        while (i <= 16) {
            if ((i >= 1) && (i <= 8)) {
                a[i] = 1;
            }
            if (i >= 2) {
                b[i] = 1;
            }
            if (i <= 14) {
                m_c[i] = 1;
            }
            i = i + 1;
        }

        TryMe(1, ref q, b, a);

        return (q == 1);
    }
}
}
