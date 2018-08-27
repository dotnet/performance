// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_TimeSpan
    {
        [Benchmark]
        public TimeSpan ctor_int_int_int() => new TimeSpan(7, 8, 10);

        [Benchmark]
        public TimeSpan FromSeconds() => TimeSpan.FromSeconds(50);
    }
}
