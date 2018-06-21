// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.CoreCLR, Categories.Benchstones, Categories.BenchI)]
public class BubbleSort2
{
    public const int Bound = 500 * 15;

    static void Inner(int[] x) {
        int limit1 = Bound - 1;
        for (int i = 1; i <= limit1; i++) {
            for (int j = i; j <= Bound; j++) {
                if (x[i] > x[j]) {
                    int temp = x[j];
                    x[j] = x[i];
                    x[i] = temp;
                }
            }
        }
    }

    // this benchmark is BAD, it should not allocate the array and check the order, but I am porting "as is"
    [Benchmark(Description = nameof(BubbleSort2))]
    public bool Test() {
        int[] x = new int[Bound + 1];
        int i, j;
        int limit;
        j = 99999;
        limit = Bound - 2;
        i = 1;
        do {
            x[i] = j & 32767;
            x[i + 1] = (j + 11111) & 32767;
            x[i + 2] = (j + 22222) & 32767;
            j = j + 33333;
            i = i + 3;
        } while (i <= limit);
        x[Bound - 1] = j;
        x[Bound] = j;

        Inner(x);

        for (i = 0; i < Bound - 1; i++) {
            if (x[i] > x[i + 1]) {
                return false;
            }
        }

        return true;
    }
}
}
