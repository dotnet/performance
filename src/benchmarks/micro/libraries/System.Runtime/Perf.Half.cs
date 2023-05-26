// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;

using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Half
    {
        public static IEnumerable<Half> Values => new Half[]
        {
            BitConverter.UInt16BitsToHalf(0x03ff),  //Maximum subnormal number in Half
            (Half)12345.0f /* same value used by other tests to compare the perf */,
            BitConverter.UInt16BitsToHalf(0x7dff)   //NaN
        };

        public static IEnumerable<float> SingleValues => new float[]
        {
            6.097555E-05f,
            12345.0f /* same value used by other tests to compare the perf */,
            65520.0f,   //Minimum value that is infinity in Half
            float.NaN
        };

        [Benchmark]
        [ArgumentsSource(nameof(SingleValues))]
        public Half SingleToHalf(float value) => (Half)value;

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public float HalfToSingle(Half value) => (float)value;
    }
}
