// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Decimal
    {
        public static IEnumerable<object> Values => new object[]
        {
            new decimal(1.23456789E+5)
        };

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public string Decimal_ToString(decimal value) => value.ToString();
    }
}