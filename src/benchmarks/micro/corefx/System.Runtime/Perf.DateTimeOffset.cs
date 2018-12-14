// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_DateTimeOffset
    {
        DateTimeOffset date1 = new DateTimeOffset(new DateTime(1996, 6, 3, 22, 15, 0), new TimeSpan(5, 0, 0));
        DateTimeOffset date2 = new DateTimeOffset(new DateTime(1996, 12, 6, 13, 2, 0), new TimeSpan(5, 0, 0));
        
        [Benchmark]
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

        [Benchmark(Description = "ToString")]
        [ArgumentsSource(nameof(ToString_MemberData))]
        public string ToString_str(string format) => date1.ToString(format);

        [Benchmark]
        public TimeSpan op_Subtraction() => date1 - date2;
    }
}
