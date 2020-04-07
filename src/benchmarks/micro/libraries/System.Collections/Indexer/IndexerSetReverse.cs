// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)] // this benchmark does not belong to CoreFX because it's more a codegen benchmark
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class IndexerSetReverse<T>
    {
        [Params(Utils.DefaultCollectionSize)] 
        public int Size;

        private T[] _array;
        private List<T> _list;

        [GlobalSetup]
        public void Setup()
        {
            _array = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _list = new List<T>(_array);
        }

        [Benchmark]
        public T[] Array()
        {
            var array = _array;
            for (int i = array.Length; --i >= 0;)
                array[i] = default;
            return array;
        }
        
        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public T Span()
        {
            T result = default;
            var span = new Span<T>(_array);
            for (int i = span.Length; --i >= 0;)
                span[i] = default;
            return result;
        }

        [Benchmark]
        public List<T> List()
        {
            var list = _list;
            for (int i = list.Count; --i >= 0;)
                list[i] = default;
            return list;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.Runtime, Categories.Virtual)]
        public IList<T> IList() => Set(_list);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private IList<T> Set(IList<T> collection)
        {
            for (int i = collection.Count; --i >= 0;)
                collection[i] = default;
            return collection;
        }
    }
}