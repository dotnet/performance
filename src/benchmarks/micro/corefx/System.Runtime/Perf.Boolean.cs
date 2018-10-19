// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Boolean
    {
        [Benchmark(Description = "Parse")]
        public bool Parse_str() => bool.Parse(bool.TrueString); 

        [Benchmark(Description = "ToString")]
        public string ToString_() => true.ToString();
    }
}
