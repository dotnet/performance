using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreCLR, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    [InvocationCount(InvocationsPerIteration)]
    public class Sort<T>
    {
        private const int InvocationsPerIteration = 1000;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private int _iterationIndex = 0;
        private T[] _values;
        
        private T[][] _arrays;
        private List<T>[] _lists;

        [GlobalSetup]
        public void Setup() => _values = UniqueValuesGenerator.GenerateArray<T>(Size);
        
        [IterationCleanup]
        public void CleanupIteration() => _iterationIndex = 0; // after every iteration end we set the index to 0

        [IterationSetup(Target = nameof(Array))]
        public void SetupArrayIteration() => Utils.FillArrays(ref _arrays, InvocationsPerIteration, _values);

        [Benchmark]
        public void Array() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size);

        [IterationSetup(Target = nameof(List))]
        public void SetupListIteration() => Utils.FillCollections(ref _lists, InvocationsPerIteration, _values);

        [Benchmark]
        public void List() => _lists[_iterationIndex++].Sort();

        [BenchmarkCategory(Categories.LINQ)]
        [Benchmark]
        public int LinqQuery()
        {
            int count = 0;
            foreach (var _ in (from value in _values orderby value ascending select value))
                count++;
            return count;
        }

        [BenchmarkCategory(Categories.LINQ)]
        [Benchmark]
        public int LinqOrderByExtension()
        {
            int count = 0;
            foreach (var _ in _values.OrderBy(value => value)) // we can't use .Count here because it has been optimized for icollection.OrderBy().Count()
                count++;
            return count;
        }
    }
}