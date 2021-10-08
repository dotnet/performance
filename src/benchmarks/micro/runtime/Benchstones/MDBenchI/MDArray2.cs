// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.MDBenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.MDBenchI)]
public class MDArray2
{
    public const int Iterations = 500000;

    static void Initialize(int[,,] s) {
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                for (int k = 0; k < 10; k++) {
                    s[i,j,k] = (2 * i) - (3 * j) + (5 * k);
                }
            }
        }
    }

    static bool VerifyCopy(int[,,] s, int[,,] d) {
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                for (int k = 0; k < 10; k++) {
                    if (s[i,j,k] != d[i,j,k]) {
                        return false;
                    }
                }
            }
        }

        return true;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench(int loop) {

        int[,,] s = new int[10, 10, 10];
        int[,,] d = new int[10, 10, 10];

        Initialize(s);

        for (; loop != 0; loop--) {
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++) {
                    for (int k = 0; k < 10; k++) {
                        d[i,j,k] = s[i,j,k];
                    }
                }
            }
        }

        bool result = VerifyCopy(s, d);

        return result;
    }

    [Benchmark(Description = nameof(MDArray2))]
    public bool Test() => Bench(Iterations);
}
}
