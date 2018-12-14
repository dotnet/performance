// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Enum
    {
        [Flags]
        public enum Colors
        {
            Red = 0x1,
            Orange = 0x2,
            Yellow = 0x4,
            Green = 0x8,
            Blue = 0x10
        }

        [Params("Red", "Red, Orange, Yellow, Green, Blue")] // test both a single value and multiple values
        public string Text;

        [Benchmark]
        public Colors Parse() => (Colors)Enum.Parse(typeof(Colors), Text);

        [Benchmark]
        public bool TryParseGeneric() => Enum.TryParse<Colors>(Text, out _);
    }
}
