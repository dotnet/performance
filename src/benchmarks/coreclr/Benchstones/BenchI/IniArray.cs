// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using BenchmarkDotNet.Attributes;
using System;
using System.Runtime.CompilerServices;




namespace Benchstone.BenchI
{
public  class IniArray
{
    public const int Iterations = 10000000;

    const int Allotted = 16;

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
