﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections.Concurrent
{
    [BenchmarkCategory(Categories.Libraries, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class Count<T>
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
        public int Dictionary() => _dictionary.Count;
        
        [Benchmark]
        public int Queue() => _queue.Count;

        [Benchmark]
        public int Queue_EnqueueCountDequeue()
        {
            _queue.Enqueue(default);
            int c = _queue.Count;
            _queue.TryDequeue(out _);
            return c;
        }

        [Benchmark]
        public int Stack() => _stack.Count;
        
        [Benchmark]
        public int Bag() => _bag.Count;
    }
}
