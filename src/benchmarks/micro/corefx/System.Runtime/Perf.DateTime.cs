// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_DateTime
    {
        DateTime date1 = new DateTime(1996, 6, 3, 22, 15, 0);
        DateTime date2 = new DateTime(1996, 12, 6, 13, 2, 0);
        
        [Benchmark]
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
        public string ToString(string format) => date1.ToString(format);

        [Benchmark]
        public TimeSpan op_Subtraction() => date1 - date2;
    }
}
