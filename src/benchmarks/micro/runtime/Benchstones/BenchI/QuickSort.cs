// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.BenchI)]
public class QuickSort
{
    const int MAXNUM = 200;
    const int MODULUS = 0x20000;
    const int C = 13849;
    const int A = 25173;
    static int s_seed = 7;

    static int Random(int size) {
        unchecked {
            s_seed = s_seed * A + C;
        }
        return (s_seed % size);
    }

    static void Quick(int lo, int hi, int[] arr) {

        int i, j;
        int pivot, temp;

        if (lo < hi) {
            for (i = lo, j = hi, pivot = arr[hi]; i < j;) {
                while (i < j && arr[i] <= pivot){
                    ++i;
                }
                while (j > i && arr[j] >= pivot) {
                    --j;
                }
                if (i < j) {
                    temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }

            // need to swap the pivot and a[i](or a[j] as i==j) so
            // that the pivot will be at its final place in the sorted array

            if (i != hi) {
                temp = arr[i];
                arr[i] = pivot;
                arr[hi] = temp;
            }
            Quick(lo, i - 1, arr);
            Quick(i + 1, hi, arr);
        }
    }

    [Benchmark(Description = nameof(QuickSort))]
    public bool Test() {

        int[] buffer = new int[MAXNUM];

        for (int j = 0; j < MAXNUM; ++j) {
            int temp = Random(MODULUS);
            if (temp < 0){
                temp = (-temp);
            }
            buffer[j] = temp;
        }

        Quick(0, MAXNUM - 1, buffer);

        for (int j = 0; j < MAXNUM - 1; ++j) {
            if (buffer[j] > buffer[j+1]) {
                return false;
            }
        }

        return true;
    }
}
}
