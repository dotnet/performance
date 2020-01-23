// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.BenchI)]
public class Pi
{
    private int[] a = new int[3340];
    
    static int[] ComputePi(int[] a) {

        int d = 4;
        int r = 10000;
        int n = 251;
        int m = (int)(3.322 * n * d);
        int[] digits = new int[n];
        int i, k, q;

        for (i = 0; i <= m; i++) {
            a[i] = 2;
        }

        a[m] = 4;

        for (i = 1; i <= n; i++) {
            q = 0;
            for (k = m; k > 0L; k--) {
                a[k] = a[k] * r + q;
                q = a[k] / (2 * k + 1);
                a[k] -= (2 * k + 1) * q;
                q *= k;
            }
            a[0] = a[0] * r + q;
            q = a[0] / r;
            a[0] -= q * r;
            digits[i-1] = q;
        }

        return digits;
    }

    [Benchmark]
    public bool Test() {
        int[] digits = ComputePi(a);
        return (digits[0] == 3 && digits[1] == 1415 && digits[2] == 9265 && digits[250] == 1989);
    }
}
}
