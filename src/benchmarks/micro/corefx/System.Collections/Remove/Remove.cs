// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    [InvocationCount(InvocationsPerIteration)]
    public class Remove<T>
    {
        private const int InvocationsPerIteration = 1000;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private int _iterationIndex = 0;
        private T[] _keys;

        private List<T>[] _lists;
        private LinkedList<T>[] _linkedLists;
        private HashSet<T>[] _hashSets;
        private Dictionary<T, T>[] _dictionaries;
        private SortedList<T, T>[] _sortedLists;
        private SortedSet<T>[] _sortedSets;
        private SortedDictionary<T, T>[] _sortedDictionaries;
        private Stack<T>[] _stacks;
        private Queue<T>[] _queues;

        [GlobalSetup]
        public void Setup() => _keys = ValuesGenerator.ArrayOfUniqueValues<T>(Size);

        [IterationCleanup]
        public void CleanupIteration() => _iterationIndex = 0; // after every iteration end we set the index to 0

        [IterationSetup(Targets = new []{ nameof(List), nameof(ICollection) })]
        public void SetupListIteration() => Utils.FillCollections(ref _lists, InvocationsPerIteration, _keys);

        [Benchmark]
        public void List()
        {
            var list = _lists[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                list.Remove(key);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.CoreCLR, Categories.Virtual)]
        public void ICollection() => RemoveFromCollection(_lists[_iterationIndex++]);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RemoveFromCollection(ICollection<T> collection)
        {
            var keys = _keys;
            foreach (var key in keys)
                collection.Remove(key);
        }

        [IterationSetup(Target = nameof(LinkedList))]
        public void SetupLinkedListIteration() => Utils.FillCollections(ref _linkedLists, InvocationsPerIteration, _keys);

        [Benchmark]
        public void LinkedList()
        {
            var linkedlist = _linkedLists[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                linkedlist.Remove(key);
        }

        [IterationSetup(Target = nameof(HashSet))]
        public void SetupHashSetIteration() => Utils.FillCollections(ref _hashSets, InvocationsPerIteration, _keys);

        [Benchmark]
        public void HashSet()
        {
            var hashset = _hashSets[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                hashset.Remove(key);
        }

        [IterationSetup(Target = nameof(Dictionary))]
        public void SetupDictionaryIteration() => Utils.FillDictionaries(ref _dictionaries, InvocationsPerIteration, _keys);

        [Benchmark]
        public void Dictionary()
        {
            var dictionary = _dictionaries[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                dictionary.Remove(key);
        }

        [IterationSetup(Target = nameof(SortedList))]
        public void SetupSortedListIteration() => Utils.FillDictionaries(ref _sortedLists, InvocationsPerIteration, _keys);

        [Benchmark]
        public void SortedList()
        {
            var sortedlist = _sortedLists[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                sortedlist.Remove(key);
        }

        [IterationSetup(Target = nameof(SortedSet))]
        public void SetupSortedSetIteration() => Utils.FillCollections(ref _sortedSets, InvocationsPerIteration, _keys);

        [Benchmark]
        public void SortedSet()
        {
            var sortedset = _sortedSets[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                sortedset.Remove(key);
        }

        [IterationSetup(Target = nameof(SortedDictionary))]
        public void SetupSortedDictionaryIteration() => Utils.FillDictionaries(ref _sortedDictionaries, InvocationsPerIteration, _keys);

        [Benchmark]
        public void SortedDictionary()
        {
            var sorteddictionary = _sortedDictionaries[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                sorteddictionary.Remove(key);
        }
        
        [IterationSetup(Target = nameof(Stack))]
        public void SetupStackIteration() => Utils.FillStacks(ref _stacks, InvocationsPerIteration, _keys);

        [Benchmark]
        public void Stack()
        {
            var stack = _stacks[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys) // we don't need to iterate over keys but to compare apples to apples we do
                stack.Pop();
        }
        
        [IterationSetup(Target = nameof(Queue))]
        public void SetupQueueIteration() => Utils.FillQueues(ref _queues, InvocationsPerIteration, _keys);
        
        [Benchmark]
        public void Queue()
        {
            var queue = _queues[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys) // we don't need to iterate over keys but to compare apples to apples we do
                queue.Dequeue();
        }
    }
}