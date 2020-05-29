// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class ContainsTrueComparer<T> where T : IEquatable<T>
    {
        private sealed class WrapDefaultComparer : IEqualityComparer<T>, IComparer<T>
        {
            public static WrapDefaultComparer Instance { get; } = new WrapDefaultComparer();
            public int Compare(T x, T y) => Comparer<T>.Default.Compare(x, y);
            public bool Equals(T x, T y) => EqualityComparer<T>.Default.Equals(x, y);
            public int GetHashCode(T obj) => EqualityComparer<T>.Default.GetHashCode(obj);
        }

        private T[] _found;

        private HashSet<T> _hashSet;
        private SortedSet<T> _sortedSet;
        private ImmutableHashSet<T> _immutableHashSet;
        private ImmutableSortedSet<T> _immutableSortedSet;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _found = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _hashSet = new HashSet<T>(_found, WrapDefaultComparer.Instance);
            _sortedSet = new SortedSet<T>(_found, WrapDefaultComparer.Instance);
            _immutableHashSet = Immutable.ImmutableHashSet.CreateRange(_found).WithComparer(WrapDefaultComparer.Instance);
            _immutableSortedSet = Immutable.ImmutableSortedSet.CreateRange(_found).WithComparer(WrapDefaultComparer.Instance);
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = default;
            HashSet<T> collection = _hashSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool SortedSet()
        {
            bool result = default;
            SortedSet<T> collection = _sortedSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableHashSet()
        {
            bool result = default;
            ImmutableHashSet<T> collection = _immutableHashSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ImmutableSortedSet()
        {
            bool result = default;
            ImmutableSortedSet<T> collection = _immutableSortedSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }
    }
}