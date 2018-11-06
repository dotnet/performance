// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class IterateFor<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _array;
        private List<T> _list;
        private IList<T> _ilist;
        private ImmutableArray<T> _immutablearray;
        private ImmutableList<T> _immutablelist;
        private ImmutableSortedSet<T> _immutablesortedset;

        [GlobalSetup(Target = nameof(Array))]
        public void SetupArray() => _array = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [Benchmark]
        public T Array()
        {
            T result = default;
            var collection = _array;
            for (int i = 0; i < collection.Length; i++)
                result = collection[i];
            return result;
        }

        [GlobalSetup(Target = nameof(Span))]
        public void SetupSpan() => _array = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public T Span()
        {
            T result = default;
            var collection = new Span<T>(_array);
            for (int i = 0; i < collection.Length; i++)
                result = collection[i];
            return result;
        }

        [GlobalSetup(Target = nameof(ReadOnlySpan))]
        public void SetupReadOnlySpan() => _array = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public T ReadOnlySpan()
        {
            T result = default;
            var collection = new ReadOnlySpan<T>(_array);
            for (int i = 0; i < collection.Length; i++)
                result = collection[i];
            return result;
        }

        [GlobalSetup(Target = nameof(List))]
        public void SetupList() => _list = new List<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T List()
        {
            T result = default;
            var collection = _list;
            for(int i = 0; i < collection.Count; i++)
                result = collection[i];
            return result;;
        }

        [GlobalSetup(Target = nameof(IList))]
        public void SetupIList() => _ilist = new List<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public T IList() => Get(_ilist);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Get(IList<T> collection)
        {
            T result = default;
            for (int i = 0; i < collection.Count; i++)
                result = collection[i];
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableArray))]
        public void SetupImmutableArray() => _immutablearray = Immutable.ImmutableArray.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableArray()
        {
            T result = default;
            var collection = _immutablearray;
            for(int i = 0; i < collection.Length; i++)
                result = collection[i];
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableList))]
        public void SetupImmutableList() => _immutablelist = Immutable.ImmutableList.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableList()
        {
            T result = default;
            var collection = _immutablelist;
            for(int i = 0; i < collection.Count; i++)
                result = collection[i];
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableSortedSet))]
        public void SetupImmutableSortedSet() => _immutablesortedset = Immutable.ImmutableSortedSet.CreateRange<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public T ImmutableSortedSet()
        {
            T result = default;
            var collection = _immutablesortedset;
            for(int i = 0; i < collection.Count; i++)
                result = collection[i];
            return result;
        }
    }
}