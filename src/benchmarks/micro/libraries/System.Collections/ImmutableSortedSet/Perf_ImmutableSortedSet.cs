// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    public class Perf_ImmutableSortedSet
    {
        private ImmutableSortedSet<int> _set;

        [GlobalSetup]
        public void Setup() => _set = ImmutableSortedSet.CreateRange(Enumerable.Range(0, 100_000));

        [Benchmark]
        public int Min() => _set.Min;

        [Benchmark]
        public int Max() => _set.Max;
    }
}
