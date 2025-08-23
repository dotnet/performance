// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type, Array sort in native code
    [GenericTypeArguments(typeof(string))] // reference type, Array sort in native code
    [GenericTypeArguments(typeof(IntStruct))] // custom value type, sort in managed code
    [GenericTypeArguments(typeof(IntClass))] // custom reference type, sort in managed code
    [GenericTypeArguments(typeof(BigStruct))] // custom value type, sort in managed code
    [InvocationCount(InvocationsPerIteration)]
    [MinWarmupCount(6, forceAutoWarmup: true)] // when InvocationCount is set, BDN does not run Pilot Stage, so to get the code promoted to Tier 1 before Actual Workload, we enforce more Warmups
    public class Sort<T> where T : IComparable<T>
    {
        private static readonly ComparableComparerClass _comparableComparerClass = new ComparableComparerClass();
        private const int InvocationsPerIteration = 5000;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private int _iterationIndex = 0;
        private T[] _values;
        private T[][] _arrays;
        private List<T>[] _lists;
        private ImmutableArray<T> _immutableArray;
        private ImmutableList<T> _immutableList;

        [GlobalSetup]
        public void Setup() => _values = GenerateValues();

        [IterationCleanup]
        public void CleanupIteration() => _iterationIndex = 0; // after every iteration end we set the index to 0

        [IterationSetup(Targets = new []{ nameof(Array), nameof(Array_ComparerClass),
            nameof(Array_ComparerStruct), nameof(Array_Comparison) })]
        public void SetupArrayIteration() => Utils.FillArrays(ref _arrays, InvocationsPerIteration, _values);

        [Benchmark]
        public void Array() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size);

        [Benchmark]
        public void Array_ComparerClass() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size, _comparableComparerClass);

        [Benchmark]
        public void Array_ComparerStruct() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size, new ComparableComparerStruct());

        [Benchmark]
        public void Array_Comparison() => System.Array.Sort(_arrays[_iterationIndex++], (x, y) => x.CompareTo(y));

        [IterationSetup(Target = nameof(List))]
        public void SetupListIteration() => Utils.ClearAndFillCollections(ref _lists, InvocationsPerIteration, _values);

        [Benchmark]
        public void List() => _lists[_iterationIndex++].Sort();

        [GlobalSetup(Target = nameof(ImmutableArray))]
        public void SetupImmutableArray() =>
            _immutableArray = Immutable.ImmutableArray.CreateRange(GenerateValues());

        [Benchmark]
        public ImmutableArray<T> ImmutableArray() => _immutableArray.Sort();

        [GlobalSetup(Target = nameof(ImmutableList))]
        public void SetupImmutableList() =>
            _immutableList = Immutable.ImmutableList.CreateRange(GenerateValues());

        [Benchmark]
        public ImmutableList<T> ImmutableList() => _immutableList.Sort();

        [BenchmarkCategory(Categories.LINQ)]
        [Benchmark]
        public int LinqQuery()
        {
            int count = 0;
            foreach (var _ in (from value in _values orderby value ascending select value))
                count++;
            return count;
        }

        [BenchmarkCategory(Categories.LINQ)]
        [Benchmark]
        public int LinqOrderByExtension()
        {
            int count = 0;
            foreach (var _ in _values.OrderBy(value => value)) // we can't use .Count here because it has been optimized for icollection.OrderBy().Count()
                count++;
            return count;
        }

        private T[] GenerateValues()
        {
            if (typeof(T) == typeof(IntStruct))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new IntStruct(v)).ToArray();
            }
            else if (typeof(T) == typeof(IntClass))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new IntClass(v)).ToArray();
            }
            else if (typeof(T) == typeof(BigStruct))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new BigStruct(v)).ToArray();
            }
            else
            {
                return ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            }
        }

        private sealed class ComparableComparerClass : IComparer<T>
        {
            public int Compare(T x, T y) => x.CompareTo(y);
        }

        private readonly struct ComparableComparerStruct : IComparer<T>
        {
            public int Compare(T x, T y) => x.CompareTo(y);
        }
    }
}
