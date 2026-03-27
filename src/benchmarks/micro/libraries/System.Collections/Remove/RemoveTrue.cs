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
    public class RemoveTrue<T>
    {
        private T[] _keys;
        private HashSet<T> _hashSet;
        private Dictionary<T, T> _dictionary;

        [Params(Utils.DefaultCollectionSize, 8192)]
        public int Size;

        [GlobalSetup(Target = nameof(HashSet))]
        public void SetupHashSet()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _hashSet = new HashSet<T>(_keys);
        }

        [Benchmark]
        public bool HashSet()
        {
            bool result = false;
            var collection = _hashSet;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                result = collection.Remove(keys[i]);
                collection.Add(keys[i]);
            }
            return result;
        }

        [GlobalSetup(Target = nameof(Dictionary))]
        public void SetupDictionary()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _dictionary = _keys.ToDictionary(k => k, k => default(T)!);
        }

        [Benchmark]
        public bool Dictionary()
        {
            bool result = false;
            var collection = _dictionary;
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                result = collection.Remove(keys[i]);
                collection[keys[i]] = default!;
            }
            return result;
        }
    }
}
