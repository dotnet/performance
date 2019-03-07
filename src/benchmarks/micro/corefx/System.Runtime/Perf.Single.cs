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
    [BenchmarkCategory(Categories.CoreFX)]
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
        [ArgumentsSource(nameof(Values))]
        public string ToString(float value) => value.ToString(); 

        public IEnumerable<object[]> ToStringWithCultureInfoArguments() 
            => Values.Select(value => new object[] { value, new CultureInfo("zh") });

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithCultureInfoArguments))]
        public string ToStringWithCultureInfo(float value, CultureInfo culture)
            => value.ToString(culture);

        public IEnumerable<object[]> ToStringWithFormat()
            => _formats.SelectMany(format => Values.Select(value => new object[] { value, format }));

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithFormat))]
        public string ToStringWithFormat(float value, string format)
            => value.ToString(format);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public float Parse(string value) => float.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParse(string value) => float.TryParse(value, out _);
    }
}