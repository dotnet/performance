// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Linq.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.LINQ)]
    public class ImmutableArrayExtensions
    {
        private ImmutableArray<int> _immutableArray;

        [GlobalSetup]
        public void GlobalSetup() => _immutableArray = ImmutableArray.CreateRange(Enumerable.Range(0, LinqTestData.Size));

        public IEnumerable<object> Arguments()
        {
            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.ICollection;
            yield return LinqTestData.IList;
            yield return LinqTestData.Array;
            yield return LinqTestData.List;
        }

        [Benchmark]
        [ArgumentsSource(nameof(Arguments))]
        [MemoryRandomization]
        public bool SequenceEqual(LinqTestData input) => _immutableArray.SequenceEqual(input.Collection);

    }
}
