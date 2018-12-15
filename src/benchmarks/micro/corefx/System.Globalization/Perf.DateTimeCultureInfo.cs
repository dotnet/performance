// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Performance tests for converting DateTime to different CultureInfos
    /// 
    /// Primary methods affected: Parse, ToString
    /// </summary>
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_DateTimeCultureInfo
    {
        private readonly DateTime _time = DateTime.Now;
        
        public IEnumerable<object> Cultures()
        {
            yield return new CultureInfo("fr");
            yield return new CultureInfo("da");
            yield return new CultureInfo("ja");
            yield return new CultureInfo("");
        }

        [Benchmark]
        [ArgumentsSource(nameof(Cultures))]
        public string ToString(CultureInfo culturestring) // the argument is called "culturestring" to keep benchmark ID, do NOT rename it
            => _time.ToString(culturestring);

        [Benchmark]
        [ArgumentsSource(nameof(Cultures))]
        public DateTime Parse(CultureInfo culturestring)
            => DateTime.Parse("10/10/2010 12:00:00 AM", culturestring);
    }
}
