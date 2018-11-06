// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Collections.Concurrent
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class IsEmpty<T>
    {
        private ConcurrentDictionary<T, T> _dictionary;
        private ConcurrentQueue<T> _queue;
        private ConcurrentStack<T> _stack;
        private ConcurrentBag<T> _bag;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        [GlobalSetup]
        public void Setup()
        {
            var values = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            
            _dictionary = new ConcurrentDictionary<T, T>(values.ToDictionary(v => v, v => v));
            _queue = new ConcurrentQueue<T>(values);
            _stack = new ConcurrentStack<T>(values);
            _bag = new ConcurrentBag<T>(values);
        }

        [Benchmark]
        public bool Dictionary() => _dictionary.IsEmpty;
        
        [Benchmark]
        public bool Queue() => _queue.IsEmpty;
        
        [Benchmark]
        public bool Stack() => _stack.IsEmpty;
        
        [Benchmark]
        public bool Bag() => _bag.IsEmpty;
    }
}