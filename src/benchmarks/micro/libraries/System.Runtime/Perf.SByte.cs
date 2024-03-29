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
    public class Perf_SByte
    {
        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        public static IEnumerable<object> Values => new object[]
        {
            sbyte.MinValue,
            sbyte.MaxValue
        };

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public sbyte Parse(string value) => sbyte.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParse(string value) => sbyte.TryParse(value, out _);

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string ToString(sbyte value) => value.ToString();

#if NET7_0_OR_GREATER
        [Benchmark]
        [Arguments(1, -1)]
        public sbyte CopySign(sbyte value, sbyte sign) => sbyte.CopySign(value, sign);
#endif
    }
}
