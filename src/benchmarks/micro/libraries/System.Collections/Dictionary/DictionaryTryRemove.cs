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
    [GenericTypeArguments(typeof(int), typeof(int))]             // small value type key+value
    [GenericTypeArguments(typeof(string), typeof(string))]       // reference type key+value
    [GenericTypeArguments(typeof(Guid), typeof(int))]            // larger value type key
    public class DictionaryTryRemove<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _dictionary;
        private TKey[] _keys;

        [Params(Utils.DefaultCollectionSize, 8192)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<TKey>(Size);
            _dictionary = _keys.ToDictionary(k => k, k => default(TValue)!);
        }

        [Benchmark]
        public bool TryRemove_Hit()
        {
            var dict = _dictionary;
            var keys = _keys;
            bool result = false;
            for (int i = 0; i < keys.Length; i++)
            {
                result = dict.Remove(keys[i], out _);
                dict.Add(keys[i], default!);
            }
            return result;
        }
    }
}
