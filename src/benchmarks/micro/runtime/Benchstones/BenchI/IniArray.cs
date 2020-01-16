// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Benchstone.BenchI
{
[BenchmarkCategory(Categories.CoreCLR, Categories.Benchstones, Categories.BenchI)]
public  class IniArray
{
    public const int Iterations = 10000000;

    const int Allotted = 16;

    /// <summary>
    /// this benchmark is very dependent on loop alignment
    /// </summary>
    [Benchmark(Description = nameof(IniArray))]
    public char[] Test() {
        char[] workarea = new char[Allotted];
        for (int i = 0; i < Iterations; i++) {
            for (int j = 0; j < Allotted; j++) {
                workarea[j] = ' ';
            }
        }
        return workarea;
    }
}
}
