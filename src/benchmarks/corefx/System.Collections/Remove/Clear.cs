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
    public class Clear<T>
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
        public void List() => _lists[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(LinkedList))]
        public void SetupLinkedListIteration() => _linkedLists = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new LinkedList<T>(_keys)).ToArray();

        [Benchmark]
        public void LinkedList() => _linkedLists[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(HashSet))]
        public void SetupHashSetIteration() => _hashSets = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new HashSet<T>(_keys)).ToArray();

        [Benchmark]
        public void HashSet() => _hashSets[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(Dictionary))]
        public void SetupDictionaryIteration() => _dictionaries = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new Dictionary<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void Dictionary() => _dictionaries[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(SortedList))]
        public void SetupSortedListIteration() => _sortedLists = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new SortedList<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void SortedList() => _sortedLists[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(SortedSet))]
        public void SetupSortedSetIteration() => _sortedSets = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new SortedSet<T>(_keys)).ToArray();

        [Benchmark]
        public void SortedSet() => _sortedSets[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(SortedDictionary))]
        public void SetupSortedDictionaryIteration() => _sortedDictionaries = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new SortedDictionary<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void SortedDictionary() => _sortedDictionaries[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(ConcurrentDictionary))]
        public void SetupConcurrentDictionaryIteration() => _concurrentDictionaries = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new ConcurrentDictionary<T, T>(_keys.ToDictionary(i => i, i => i))).ToArray();

        [Benchmark]
        public void ConcurrentDictionary() => _concurrentDictionaries[_iterationIndex++].Clear();
        
        [IterationSetup(Target = nameof(Stack))]
        public void SetupStackIteration() => _stacks = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new Stack<T>(_keys)).ToArray();

        [Benchmark]
        public void Stack() => _stacks[_iterationIndex++].Clear();
        
        [IterationSetup(Target = nameof(ConcurrentStack))]
        public void SetupConcurrentStackIteration() => _concurrentStacks = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new ConcurrentStack<T>(_keys)).ToArray();

        [Benchmark]
        public void ConcurrentStack() => _concurrentStacks[_iterationIndex++].Clear();
        
        [IterationSetup(Target = nameof(Queue))]
        public void SetupQueueIteration() => _queues = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new Queue<T>(_keys)).ToArray();

        [Benchmark]
        public void Queue() => _queues[_iterationIndex++].Clear();

#if !NET461 // API added in .NET Core 2.0
        [IterationSetup(Target = nameof(ConcurrentQueue))]
        public void SetupConcurrentQueueIteration() => _concurrentQueues = Enumerable.Range(0, InvocationsPerIteration).Select(_ => new ConcurrentQueue<T>(_keys)).ToArray();

        [Benchmark]
        public void ConcurrentQueue() => _concurrentQueues[_iterationIndex++].Clear();
#endif
    }
}