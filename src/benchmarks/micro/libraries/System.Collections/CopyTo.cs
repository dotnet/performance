// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type
    [GenericTypeArguments(typeof(string))] // reference type
    public class CopyTo<T>
    {
        [Params(Utils.DefaultCollectionSize * 4)]
        public int Size;

        private T[] _array;
        private List<T> _list;
        private ImmutableArray<T> _immutablearray;
        private T[] _destination;

        [GlobalSetup]
        public void Setup()
        {
            _array = ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            _list = new List<T>(_array);
            _immutablearray = Immutable.ImmutableArray.CreateRange(_array);
            _destination = new T[Size];
        }

        [Benchmark]
        public void Array() => System.Array.Copy(_array, _destination, Size);

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public void Span() => new Span<T>(_array).CopyTo(new Span<T>(_destination));

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public void ReadOnlySpan() => new ReadOnlySpan<T>(_array).CopyTo(new Span<T>(_destination));

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public void Memory() => new Memory<T>(_array).CopyTo(new Memory<T>(_destination));

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public void ReadOnlyMemory() => new ReadOnlyMemory<T>(_array).CopyTo(new Memory<T>(_destination));

        [Benchmark]
        public void List() => _list.CopyTo(_destination);

        [Benchmark]
        public void ImmutableArray() => _immutablearray.CopyTo(_destination);
    }
}