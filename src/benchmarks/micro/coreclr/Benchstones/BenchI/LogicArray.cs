// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.CoreCLR, Categories.Benchstones, Categories.BenchI)]
public class LogicArray
{
    public const int Iterations = 3000;

    struct Workarea
    {
        public int X;
        public int[][] A;
    }

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    static bool Inner(ref Workarea cmn) {
        int i, j, k;
        cmn.X = 0;
        for (i = 1; i <= 50; i++) {
            for (j = 1; j <= 50; j++) {
                cmn.A[i][j] = 1;
            }
        }
        for (k = 1; k <= 50; k++) {
            for (j = 1; j <= 50; j++) {
                i = 1;
                do {
                    cmn.X = cmn.X | cmn.A[i][j] & cmn.A[i + 1][k];
                    i = i + 2;
                } while (i <= 50);
            }
        }
        if (cmn.X != 1) {
            return false;
        }
        else {
            return true;
        }
    }

    [Benchmark(Description = nameof(LogicArray))]
    public bool Test() {
        Workarea cmn = new Workarea();
        cmn.X = 0;
        cmn.A = AllocArray<int>(51, 51);
        for (int n = 1; n <= Iterations; n++) {
            bool result = Inner(ref cmn);
            if (!result) {
                return false;
            }
        }

        return true;
    }
}
}
