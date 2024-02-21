// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Boolean
    {
        public static IEnumerable<object> ValidStringValues => new string[]{
            "true",
            "false",
            "TRUE",
            "False",
            " True "
        };
        public static IEnumerable<object> ValidAndInvalidStringValues => ValidStringValues.Concat(new string[]
        {
            "0",
            "Bogus"
        });
        public static IEnumerable<object> Values => new object[]
        {
            true,
            false
        };

        [Benchmark]
        [ArgumentsSource(nameof(ValidStringValues))]
        [MemoryRandomization]
        public bool Parse(string value) => bool.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(ValidAndInvalidStringValues))]
        public bool TryParse(string value) => bool.TryParse(value, out _);

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string ToString(bool value) => value.ToString();
    }
}
