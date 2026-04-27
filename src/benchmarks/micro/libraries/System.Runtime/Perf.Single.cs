// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Single
    {
        private readonly string[] _formats = { "R", "G", "G17", "E", "F50" };

        public static IEnumerable<object> Values => new object[]
        {
            float.MinValue,
            (float)12345.0 /* same value used by other tests to compare the perf */,
            float.MaxValue
        };

        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        [Benchmark]
        [Arguments(0.0f)]
        [Arguments(float.NaN)]
        public bool IsNaN(float value)
        {
            // float.IsNaN takes very little time to execute,
            // so we need to boost the execution time a bit.

            bool result = false;

            for (int i = 0; i < 1000000; i++)
            {
                result |= float.IsNaN(value);
                value += 1.0f;
            }

            return result;
        }

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public string ToString(float value) => value.ToString(); 

        public IEnumerable<object[]> ToStringWithCultureInfoArguments() 
            => Values.Select(value => new object[] { value, new CultureInfo("zh") });

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithCultureInfoArguments))]
        [MemoryRandomization]
        public string ToStringWithCultureInfo(float value, CultureInfo culture)
            => value.ToString(culture);

        public IEnumerable<object[]> ToStringWithFormatArguments()
            => _formats.SelectMany(format => Values.Select(value => new object[] { value, format }));

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithFormatArguments))]
        [MemoryRandomization]
        public string ToStringWithFormat(float value, string format)
            => value.ToString(format);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        [MemoryRandomization]
        public float Parse(string value) => float.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        [MemoryRandomization]
        public bool TryParse(string value) => float.TryParse(value, out _);
    }
}