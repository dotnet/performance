// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.BenchI)]
public class BenchE
{
    public const int Iterations = 5000000;

    private static int s_position;

    private static int Strsch(char[] s, char[] k, int ns, int nk)
    {
        int i, j;
        int start, ksave, cont;
        int kend, ssave;
        int r;

        start = 0;
        ksave = 0;
        cont = ns - nk + start;
        kend = ksave + nk - 1;
        i = 0;
        j = 0;
    top:
        while (s[i] != k[j])
        {
            // s is accessed upto cont i.e. ns - nk + 0
            if (i >= cont)
            {
                r = -1;
                goto bottom;
            }
            i = i + 1;
        }
        ssave = i;
        j = j + 1;
        while (j <= kend)
        {
            i = i + 1;
            // j <= kend, so k is accessed upto 0 + nk - 1
            if (s[i] != k[j])
            {
                i = ssave + 1;
                j = ksave;
                goto top;
            }
            j = j + 1;
        }
        r = ssave - start + 1;
    bottom:
        return r;
    }

    private static void BenchInner(char[] s, char[] k)
    {
        int ns, nk;

        ns = 120;
        nk = 15;
        s_position = Strsch(s, k, ns, nk);
    }

    [Benchmark(Description = nameof(BenchE))]
    public bool Test()
    {
        char[] s = {
            '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', 'H', 'E', 'R', 'E', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0',
            'H', 'E', 'R', 'E', ' ', 'I', 'S', ' ', 'A', ' ', 'M', 'A', 'T', 'C', 'H', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0'
        };

        char[] k = { 'H', 'E', 'R', 'E', ' ', 'I', 'S', ' ', 'A', ' ', 'M', 'A', 'T', 'C', 'H', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };

        for (int i = 0; i < Iterations; i++)
        {
            BenchInner(s, k);
        }

        return (s_position == 91);
    }
}
}
