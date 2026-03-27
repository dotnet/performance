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
    public class DictionaryContainsKey<T>
    {
        private T[] _found;
        private T[] _notFound;
        private Dictionary<T, T> _dictionary;

        [Params(Utils.DefaultCollectionSize, 8192)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            var allKeys = ValuesGenerator.ArrayOfUniqueValues<T>(Size * 2);
            _found = allKeys.Skip(Size).Take(Size).ToArray();
            _notFound = allKeys.Take(Size).ToArray();
            _dictionary = _found.ToDictionary(k => k, k => default(T)!);
        }

        [Benchmark]
        public bool ContainsKeyTrue()
        {
            bool result = default;
            Dictionary<T, T> collection = _dictionary;
            T[] found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ContainsKeyFalse()
        {
            bool result = default;
            Dictionary<T, T> collection = _dictionary;
            T[] notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.ContainsKey(notFound[i]);
            return result;
        }
    }

    /// <summary>
    /// Measures ContainsKey on a 1M-entry dictionary to capture behavior when
    /// the hash table far exceeds CPU cache. Probes only 512 keys per invocation
    /// so BDN gets enough iterations for stable statistics.
    /// </summary>
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    public class DictionaryContainsKeyLarge
    {
        private const int DictSize = 1_000_000;
        private const int ProbeCount = 512;

        private int[] _found;
        private int[] _notFound;
        private Dictionary<int, int> _dictionary;

        [GlobalSetup]
        public void Setup()
        {
            var allKeys = ValuesGenerator.ArrayOfUniqueValues<int>(DictSize + ProbeCount);
            var inDict = allKeys.Take(DictSize).ToArray();
            _found = inDict.Take(ProbeCount).ToArray();
            _notFound = allKeys.Skip(DictSize).Take(ProbeCount).ToArray();
            _dictionary = inDict.ToDictionary(k => k, k => k);
        }

        [Benchmark]
        public bool ContainsKeyTrue()
        {
            bool result = default;
            var collection = _dictionary;
            var found = _found;
            for (int i = 0; i < found.Length; i++)
                result ^= collection.ContainsKey(found[i]);
            return result;
        }

        [Benchmark]
        public bool ContainsKeyFalse()
        {
            bool result = default;
            var collection = _dictionary;
            var notFound = _notFound;
            for (int i = 0; i < notFound.Length; i++)
                result ^= collection.ContainsKey(notFound[i]);
            return result;
        }
    }
}
