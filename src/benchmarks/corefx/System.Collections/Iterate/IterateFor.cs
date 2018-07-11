using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using Helpers;

namespace System.Collections
{
    [BenchmarkCategory(Categories.CoreFX, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class IterateFor<TValue>
    {
        [Params(100)]
        public int Size;

        private TValue[] _array;
        private List<TValue> _list;
        private ImmutableArray<TValue> _immutablearray;
        private ImmutableList<TValue> _immutablelist;
        private ImmutableSortedSet<TValue> _immutablesortedset;

        [GlobalSetup(Target = nameof(Array))]
        public void SetupArray() => _array = UniqueValuesGenerator.GenerateArray<TValue>(Size);

        [Benchmark]
        public TValue Array()
        {
            TValue result = default;
            var local = _array;
            for (int i = 0; i < local.Length; i++)
                result = local[i];
            return result;
        }

        [GlobalSetup(Target = nameof(List))]
        public void SetupList() => _list = new List<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue List()
        {
            TValue result = default;
            var local = _list;
            for(int i = 0; i < local.Count; i++)
                result = local[i];
            return result;;
        }

        [GlobalSetup(Target = nameof(ImmutableArray))]
        public void SetupImmutableArray() => _immutablearray = Immutable.ImmutableArray.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableArray()
        {
            TValue result = default;
            var local = _immutablearray;
            for(int i = 0; i < local.Length; i++)
                result = local[i];
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableList))]
        public void SetupImmutableList() => _immutablelist = Immutable.ImmutableList.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableList()
        {
            TValue result = default;
            var local = _immutablelist;
            for(int i = 0; i < local.Count; i++)
                result = local[i];
            return result;
        }

        [GlobalSetup(Target = nameof(ImmutableSortedSet))]
        public void SetupImmutableSortedSet() => _immutablesortedset = Immutable.ImmutableSortedSet.CreateRange<TValue>(UniqueValuesGenerator.GenerateArray<TValue>(Size));

        [Benchmark]
        public TValue ImmutableSortedSet()
        {
            TValue result = default;
            var local = _immutablesortedset;
            for(int i = 0; i < local.Count; i++)
                result = local[i];
            return result;
        }
    }
}