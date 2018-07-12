using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    [InvocationCount(InvocationsPerIteration)]
    public class Remove<T>
    {
        private const int InvocationsPerIteration = 20000;

        [Params(100)]
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
        private ConcurrentDictionary<T, T>[] _concurrentDictionaries;
        private Stack<T>[] _stacks;
        private Queue<T>[] _queues;
        private ConcurrentStack<T>[] _concurrentStacks;
        private ConcurrentQueue<T>[] _concurrentQueues;

        [GlobalSetup]
        public void Setup() => _keys = UniqueValuesGenerator.GenerateArray<T>(Size);

        [IterationCleanup]
        public void CleanupIteration() => _iterationIndex = 0; // after every iteration end we set the index to 0

        [IterationSetup(Target = nameof(List))]
        public void SetupListIteration() => _lists = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new List<T>(_keys)).ToArray();

        [Benchmark]
        public void List()
        {
            var list = _lists[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                list.Remove(key);
        }

        [IterationSetup(Target = nameof(LinkedList))]
        public void SetupLinkedListIteration() => _linkedLists = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new LinkedList<T>(_keys)).ToArray();

        [Benchmark]
        public void LinkedList()
        {
            var linkedlist = _linkedLists[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                linkedlist.Remove(key);
        }

        [IterationSetup(Target = nameof(HashSet))]
        public void SetupHashSetIteration() => _hashSets = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new HashSet<T>(_keys)).ToArray();

        [Benchmark]
        public void HashSet()
        {
            var hashset = _hashSets[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                hashset.Remove(key);
        }

        [IterationSetup(Target = nameof(Dictionary))]
        public void SetupDictionaryIteration() => _dictionaries = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new Dictionary<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void Dictionary()
        {
            var dictionary = _dictionaries[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                dictionary.Remove(key);
        }

        [IterationSetup(Target = nameof(SortedList))]
        public void SetupSortedListIteration() => _sortedLists = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new SortedList<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void SortedList()
        {
            var sortedlist = _sortedLists[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                sortedlist.Remove(key);
        }

        [IterationSetup(Target = nameof(SortedSet))]
        public void SetupSortedSetIteration() => _sortedSets = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new SortedSet<T>(_keys)).ToArray();

        [Benchmark]
        public void SortedSet()
        {
            var sortedset = _sortedSets[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                sortedset.Remove(key);
        }

        [IterationSetup(Target = nameof(SortedDictionary))]
        public void SetupSortedDictionaryIteration() => _sortedDictionaries = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new SortedDictionary<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void SortedDictionary()
        {
            var sorteddictionary = _sortedDictionaries[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                sorteddictionary.Remove(key);
        }

        [IterationSetup(Target = nameof(ConcurrentDictionary))]
        public void SetupConcurrentDictionaryIteration() => _concurrentDictionaries = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new ConcurrentDictionary<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void ConcurrentDictionary()
        {
            var concurrentdictionary = _concurrentDictionaries[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys)
                concurrentdictionary.TryRemove(key, out _);
        }
        
        [IterationSetup(Target = nameof(Stack))]
        public void SetupStackIteration() => _stacks = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new Stack<T>(_keys)).ToArray();

        [Benchmark]
        public void Stack()
        {
            var stack = _stacks[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys) // we don't need to iterate over keys but to compare apples to apples we do
                stack.Pop();
        }
        
        [IterationSetup(Target = nameof(ConcurrentStack))]
        public void SetupConcurrentStackIteration() => _concurrentStacks = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new ConcurrentStack<T>(_keys)).ToArray();
        
        [Benchmark]
        public void ConcurrentStack()
        {
            var stack = _concurrentStacks[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys) // we don't need to iterate over keys but to compare apples to apples we do
                stack.TryPop(out _);
        }
        
        [IterationSetup(Target = nameof(Queue))]
        public void SetupQueueIteration() => _queues = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new Queue<T>(_keys)).ToArray();
        
        [Benchmark]
        public void Queue()
        {
            var queue = _queues[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys) // we don't need to iterate over keys but to compare apples to apples we do
                queue.Dequeue();
        }
        
        [IterationSetup(Target = nameof(ConcurrentQueue))]
        public void SetupConcurrentQueueIteration() => _concurrentQueues = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new ConcurrentQueue<T>(_keys)).ToArray();
        
        [Benchmark]
        public void ConcurrentQueue()
        {
            var queue = _concurrentQueues[_iterationIndex++];
            var keys = _keys;
            foreach (var key in keys) // we don't need to iterate over keys but to compare apples to apples we do
                queue.TryDequeue(out _);
        }
    }
}