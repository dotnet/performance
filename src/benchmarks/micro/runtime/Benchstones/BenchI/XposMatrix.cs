// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.BenchI)]
public class XposMatrix
{
    public const int ArraySize = 100;
    
    int[][] matrixField = AllocArray<int>(ArraySize + 1, ArraySize + 1);

    static T[][] AllocArray<T>(int n1, int n2) {
        T[][] a = new T[n1][];
        for (int i = 0; i < n1; ++i) {
            a[i] = new T[n2];
        }
        return a;
    }

    static void Inner(int[][] x, int n) {
        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= n; j++) {
                int t = x[i][j];
                x[i][j] = x[j][i];
                x[j][i] = t;
            }
        }
    }

    [Benchmark(Description = nameof(XposMatrix))]
    public bool Test() {
        int[][] matrix = matrixField;
        
        int n = ArraySize;
        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= n; j++) {
                matrix[i][j] = 1;
            }
        }

        if (matrix[n][n] != 1) {
            return false;
        }

        Inner(matrix, n);

        if (matrix[n][n] != 1) {
            return false;
        }

        return true;
    }
}
}
