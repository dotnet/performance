// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Enum
    {
        private enum testEnum
        {
            Red = 1,
            Blue = 2
        }

        [Benchmark]
        public object Parse() => Enum.Parse(typeof(testEnum), "Red");
        
        [Benchmark]
        public bool TryParseGeneric() => Enum.TryParse<testEnum>("Red", out _);
    }
}
