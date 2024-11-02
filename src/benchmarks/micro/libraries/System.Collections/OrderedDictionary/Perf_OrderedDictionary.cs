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
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class Perf_OrderedDictionary<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _keys;
        private Dictionary<T, T> _dictionary;

        [GlobalSetup]
        public void SetupOrderedDictionary()
        {
            _keys = ValuesGenerator.ArrayOfUniqueValues<T>(2 * Size);
            _dictionary = _keys.Take(Size).ToDictionary(i => i, _ => default(T));
        }

        [Benchmark]
        public OrderedDictionary<T, T> AddOrUpdate()
        {
            var dictionary = new OrderedDictionary<T, T>(_dictionary);
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (dictionary.TryGetValue(key, out T value))
                {
                    dictionary[key] = value ?? key;
                }
                else
                {
                    dictionary.Add(key, key);
                }
            }
            return dictionary;
        }

        [Benchmark]
        public OrderedDictionary<T, T> AddOrUpdate2()
        {
            var dictionary = new OrderedDictionary<T, T>(_dictionary);
            var keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (!dictionary.TryAdd(key, key))
                {
                    dictionary[key] = key;
                }
            }
            return dictionary;
        }
    }
}