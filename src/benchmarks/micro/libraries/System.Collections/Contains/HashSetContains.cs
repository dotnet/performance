// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))]       // value type
    [GenericTypeArguments(typeof(string))]    // reference type
    [GenericTypeArguments(typeof(Guid))]      // larger value type
    public class HashSetContains<T>
    {
        private T[] _found;
        private T[] _notFound;
        private HashSet<T> _hashSet;

        [Params(Utils.DefaultCollectionSize, 8192)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            var allKeys = ValuesGenerator.ArrayOfUniqueValues<T>(Size * 2);
            _found = allKeys.Skip(Size).Take(Size).ToArray();
            _notFound = allKeys.Take(Size).ToArray();
            _hashSet = new HashSet<T>(_found);
        }

        [Benchmark]
        public bool ContainsTrue()
        {
            bool result = default;
            HashSet<T> collection = _hashSet;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.Contains(found[i]);
            return result;
        }

        [Benchmark]
        public bool ContainsFalse()
        {
            bool result = default;
            HashSet<T> collection = _hashSet;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.Contains(notFound[i]);
            return result;
        }
    }
}
