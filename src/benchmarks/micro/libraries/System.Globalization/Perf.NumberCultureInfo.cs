// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Globalization.Tests
{
    /// <summary>
    /// Performance tests for converting numbers to different CultureInfos
    /// </summary>
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_NumberCultureInfo
    {
        public IEnumerable<object> Cultures()
        {
            yield return new CultureInfo("fr");
            yield return new CultureInfo("da");
            yield return new CultureInfo("ja");
            yield return new CultureInfo("");
        }

        [Benchmark]
        [ArgumentsSource(nameof(Cultures))]
        [MemoryRandomization]
        public string ToString(CultureInfo culturestring) // the argument is called "culturestring" to keep benchmark ID, do NOT rename it
            => 104234.343.ToString(culturestring);
    }
}
