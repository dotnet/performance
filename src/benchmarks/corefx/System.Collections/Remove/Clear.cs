using System.Collections.Concurrent;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    [InvocationCount(InvocationsPerIteration)]
    public class Clear<T>
    {
        private const int InvocationsPerIteration = 1000;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private int _iterationIndex = 0;
        private T[] _keys;

        private T[][] _arrays;
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

        [IterationSetup(Target = nameof(Array))]
        public void SetupArrayIteration() => Utils.FillArrays(ref _arrays, InvocationsPerIteration, _keys);

        [BenchmarkCategory(Categories.CoreCLR)]
        [Benchmark]
        public void Array() => System.Array.Clear(_arrays[_iterationIndex++], 0, Size);

        [IterationSetup(Target = nameof(Span))]
        public void SetupSpanIteration() => Utils.FillArrays(ref _arrays, InvocationsPerIteration, _keys);

        [BenchmarkCategory(Categories.CoreCLR, Categories.Span)]
        [Benchmark]
        public void Span() => new Span<T>(_arrays[_iterationIndex++]).Clear();

        [IterationSetup(Target = nameof(List))]
        public void SetupListIteration() => Utils.FillCollections(ref _lists, InvocationsPerIteration, _keys);

        [Benchmark]
        public void List() => _lists[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(LinkedList))]
        public void SetupLinkedListIteration() => Utils.FillCollections(ref _linkedLists, InvocationsPerIteration, _keys);

        [Benchmark]
        public void LinkedList() => _linkedLists[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(HashSet))]
        public void SetupHashSetIteration() => Utils.FillCollections(ref _hashSets, InvocationsPerIteration, _keys);

        [Benchmark]
        public void HashSet() => _hashSets[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(Dictionary))]
        public void SetupDictionaryIteration() => Utils.FillDictionaries(ref _dictionaries, InvocationsPerIteration, _keys);

        [Benchmark]
        public void Dictionary() => _dictionaries[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(SortedList))]
        public void SetupSortedListIteration() => Utils.FillDictionaries(ref _sortedLists, InvocationsPerIteration, _keys);

        [Benchmark]
        public void SortedList() => _sortedLists[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(SortedSet))]
        public void SetupSortedSetIteration() => Utils.FillCollections(ref _sortedSets, InvocationsPerIteration, _keys);

        [Benchmark]
        public void SortedSet() => _sortedSets[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(SortedDictionary))]
        public void SetupSortedDictionaryIteration() =>  Utils.FillDictionaries(ref _sortedDictionaries, InvocationsPerIteration, _keys);

        [Benchmark]
        public void SortedDictionary() => _sortedDictionaries[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(ConcurrentDictionary))]
        public void SetupConcurrentDictionaryIteration() => Utils.FillDictionaries(ref _concurrentDictionaries, InvocationsPerIteration, _keys);

        [Benchmark]
        public void ConcurrentDictionary() => _concurrentDictionaries[_iterationIndex++].Clear();
        
        [IterationSetup(Target = nameof(Stack))]
        public void SetupStackIteration() => Utils.FillStacks(ref _stacks, InvocationsPerIteration, _keys);

        [Benchmark]
        public void Stack() => _stacks[_iterationIndex++].Clear();
        
        [IterationSetup(Target = nameof(ConcurrentStack))]
        public void SetupConcurrentStackIteration() => Utils.FillProducerConsumerCollection(ref _concurrentStacks, InvocationsPerIteration, _keys);

        [Benchmark]
        public void ConcurrentStack() => _concurrentStacks[_iterationIndex++].Clear();

        [IterationSetup(Target = nameof(Queue))]
        public void SetupQueueIteration() => Utils.FillQueues(ref _queues, InvocationsPerIteration, _keys);

        [Benchmark]
        public void Queue() => _queues[_iterationIndex++].Clear();

#if !NETFRAMEWORK // API added in .NET Core 2.0
        [IterationSetup(Target = nameof(ConcurrentQueue))]
        public void SetupConcurrentQueueIteration() => Utils.FillProducerConsumerCollection(ref _concurrentQueues, InvocationsPerIteration, _keys);

        [Benchmark]
        public void ConcurrentQueue() => _concurrentQueues[_iterationIndex++].Clear();
#endif
    }
}