// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Decimal
    {
        public static IEnumerable<object> Values => new object[]
        {
            new decimal(1.23456789E+5)
        };

        public static IEnumerable<object> StringValues
            => Values.OfType<decimal>().Select(value => value.ToString());

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string ToString(decimal value) => value.ToString();

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public decimal Parse(string value) => decimal.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParse(string value) => decimal.TryParse(value, out _);
    }
}