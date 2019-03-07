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
    public class Perf_Double
    {
        private readonly string[] _formats = { "R", "G", "G17", "E", "F50" };

        public static IEnumerable<object> Values => new object[]
        {
            double.MinValue,
            12345.0 /* same value used by other tests to compare the perf */,
            double.MaxValue
        };

        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        public IEnumerable<object[]> DefaultToStringArguments() 
            => Values.Select(value => new object[] {value, 100_000});
        
        [Benchmark]
        [ArgumentsSource(nameof(DefaultToStringArguments))]
        public string DefaultToString(double number, int innerIterations) // innerIterations argument is not used anymore but kept to preserve benchmark ID, do NOT remove it 
            => number.ToString(); 

        public IEnumerable<object[]> ToStringWithCultureInfoArguments() 
            => Values.Select(value => new object[] { new CultureInfo("zh"), value, 100_000});

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithCultureInfoArguments))]
        public string ToStringWithCultureInfo(CultureInfo cultureName, double number, int innerIterations) // the argument is called "cultureName" instead of "culture" to keep benchmark ID in BenchView, do NOT rename it
            => number.ToString(cultureName);

        public IEnumerable<object[]> ToStringWithFormat_TestData()
            => _formats.SelectMany(format => Values.Select(value => new object[] {format, value, 2_000_000}));

        [Benchmark]
        [ArgumentsSource(nameof(ToStringWithFormat_TestData))]
        public string ToStringWithFormat(string format, double number, int innerIterations) // innerIterations argument is not used anymore but kept to preserve benchmark ID, do NOT remove it  
            => number.ToString(format);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public double Parse(string value) => double.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParse(string value) => double.TryParse(value, out _);
    }
}