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
    public class RemoveFalse<T>
    {
        private T[] _missingKeys;
        private HashSet<T> _hashSet;
        private Dictionary<T, T> _dictionary;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] Setup()
        {
            var allKeys = ValuesGenerator.ArrayOfUniqueValues<T>(Size * 2);
            _missingKeys = allKeys.Take(Size).ToArray();
            return allKeys.Skip(Size).Take(Size).ToArray();
        }

        [GlobalSetup(Target = nameof(HashSet))]
        public void SetupHashSet() => _hashSet = new HashSet<T>(Setup());

        [Benchmark]
        public bool HashSet()
        {
            bool result = false;
            var collection = _hashSet;
            var keys = _missingKeys;
            for (int i = 0; i < keys.Length; i++)
                result = collection.Remove(keys[i]);
            return result;
        }

        [GlobalSetup(Target = nameof(Dictionary))]
        public void SetupDictionary()
        {
            var source = Setup();
            _dictionary = source.ToDictionary(k => k, k => default(T)!);
        }

        [Benchmark]
        public bool Dictionary()
        {
            bool result = false;
            var collection = _dictionary;
            var keys = _missingKeys;
            for (int i = 0; i < keys.Length; i++)
                result = collection.Remove(keys[i]);
            return result;
        }
    }
}
