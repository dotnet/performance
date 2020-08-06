// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.Runtime, Categories.Benchstones, Categories.JIT, Categories.BenchI)]
public class AddArray
{
    const int Size = 6000;

    public static volatile object VolatileObject;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Escape(object obj) {
        VolatileObject = obj;
    }

    [Benchmark(Description = nameof(AddArray))]
    public bool Test() {

        int[] flags1 = new int[Size + 1];
        int[] flags2 = new int[Size + 1];
        int[] flags3 = new int[Size + 1];
        int[] flags4 = new int[Size + 1];

        int j, k, l, m;

        for (j = 0; j <= Size; j++) {
            flags1[j] = 70000 + j;
            k = j;
            flags2[k] = flags1[j] + k + k;
            l = j;
            flags3[l] = flags2[k] + l + l + l;
            m = j;
            flags4[m] = flags3[l] + m + m + m + m;
        }

        for (j = 0; j <= Size; j++) {
            k = j;
            l = j;
            m = j;
            flags1[j] = flags1[j] + flags2[k] + flags3[l] + flags4[m] - flags2[k - j + l];
        }

        // Escape each flags array so that their elements will appear live-out
        Escape(flags1);
        Escape(flags2);
        Escape(flags3);
        Escape(flags4);

        return true;
    }
}
}
