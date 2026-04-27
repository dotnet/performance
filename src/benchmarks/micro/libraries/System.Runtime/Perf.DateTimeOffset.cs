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
    public class Perf_DateTimeOffset
    {
        DateTimeOffset date1 = new DateTimeOffset(new DateTime(1996, 6, 3, 22, 15, 0), new TimeSpan(5, 0, 0));
        DateTimeOffset date2 = new DateTimeOffset(new DateTime(1996, 12, 6, 13, 2, 0), new TimeSpan(5, 0, 0));
        
        [Benchmark]
        [MemoryRandomization]
        public DateTimeOffset GetNow() => DateTimeOffset.Now;

        [Benchmark]
        public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

        public static IEnumerable<string> ToString_MemberData()
        {
            yield return null;
            yield return "G";
            yield return "s";
            yield return "r";
            yield return "o";
        }

        [Benchmark]
        [ArgumentsSource(nameof(ToString_MemberData))]
        [MemoryRandomization]
        public string ToString(string format) => date1.ToString(format);

        [Benchmark]
        public TimeSpan op_Subtraction() => date1 - date2;

        public static IEnumerable<object> Values => new object[]
        {
            new DateTimeOffset(year: 2017, month: 12, day: 30, hour: 3, minute: 45, second: 22, millisecond: 950, offset: new TimeSpan(hours: -8, minutes: 0, seconds: 0))
        };

        public static IEnumerable<object> StringValues => Values.Select(value => value.ToString()).ToArray();

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        [MemoryRandomization]
        public string ToString(DateTimeOffset value) => value.ToString();

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public DateTimeOffset Parse(string value) => DateTimeOffset.Parse(value);

        [Benchmark]
        [ArgumentsSource(nameof(StringValues))]
        public bool TryParse(string value) => DateTimeOffset.TryParse(value, out _);
    }
}
