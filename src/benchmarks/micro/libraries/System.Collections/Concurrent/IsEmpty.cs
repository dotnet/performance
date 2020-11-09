// Licensed to the .NET Foundation under one or more agreements.
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
    public class IsEmpty<T>
    {
        private ConcurrentDictionary<T, T> _dictionary;
        private ConcurrentQueue<T> _queue;
        private ConcurrentStack<T> _stack;
        private ConcurrentBag<T> _bag;

        [Params(0, Utils.DefaultCollectionSize)] // IsEmpty perf can be significantly impacted by empty vs non-empty
        public int Size;

        [GlobalSetup(Target = nameof(Dictionary))]
        public void SetupDictionary() => _dictionary = new ConcurrentDictionary<T, T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size).ToDictionary(v => v, v => v));

        [Benchmark]
        public bool Dictionary() => _dictionary.IsEmpty;

        [GlobalSetup(Target = nameof(Queue))]
        public void SetupQueue() => _queue = new ConcurrentQueue<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public bool Queue() => _queue.IsEmpty;

        [GlobalSetup(Target = nameof(Stack))]
        public void SetupStack() => _stack = new ConcurrentStack<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public bool Stack() => _stack.IsEmpty;

        [GlobalSetup(Target = nameof(Bag))]
        public void SetupBag() => _bag = new ConcurrentBag<T>(ValuesGenerator.ArrayOfUniqueValues<T>(Size));

        [Benchmark]
        public bool Bag() => _bag.IsEmpty;
    }
}