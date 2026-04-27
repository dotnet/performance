// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_DateTime
    {
        DateTime date1 = new DateTime(1996, 6, 3, 22, 15, 0);
        DateTime date2 = new DateTime(1996, 12, 6, 13, 2, 0);
        object date2Boxed;

        [GlobalSetup]
        public void Setup() => date2Boxed = date2;

        [Benchmark]
        [MemoryRandomization]
        public DateTime GetNow() => DateTime.Now;

        [Benchmark]
        public DateTime GetUtcNow() => DateTime.UtcNow;

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
        public bool ObjectEquals() => date1.Equals(date2Boxed);

        [Benchmark]
        public TimeSpan op_Subtraction() => date1 - date2;

        [Benchmark]
        public DateTime ParseR() => DateTime.ParseExact("Mon, 03 Jun 1996 22:15:00 GMT", "r", null);

        [Benchmark]
        public DateTime ParseO() => DateTime.ParseExact("1996-06-03T22:15:00.0000000", "o", null);

        [Benchmark]
        public int Day() => date1.Day;

        [Benchmark]
        public int Month() => date1.Month;

        [Benchmark]
        public int Year() => date1.Year;

        [Benchmark]
        public int DayOfYear() => date1.DayOfYear;
    }
}
