﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(char))]
    [GenericTypeArguments(typeof(int))]
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class Span<T> 
        where T : struct, IComparable<T>, IEquatable<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private T[] _array, _same, _emptyWithSingleValue;
        private T _notDefaultValue;

        [GlobalSetup]
        public void Setup()
        {
            _array = ValuesGenerator.Array<T>(Size);
            _same = _array.ToArray();
        }

        [Benchmark]
        public void Clear() => new System.Span<T>(_array).Clear();
        
        [Benchmark]
        public void Fill() => new System.Span<T>(_array).Fill(default);
        
        [Benchmark]
        public void Reverse() => new System.Span<T>(_array).Reverse();

        [Benchmark]
        public T[] ToArray() => new System.Span<T>(_array).ToArray();
        
        [Benchmark]
        public bool SequenceEqual() => new System.Span<T>(_array).SequenceEqual(new System.ReadOnlySpan<T>(_same));

        [Benchmark]
        public int SequenceCompareTo() => new System.Span<T>(_array).SequenceCompareTo(new System.ReadOnlySpan<T>(_same));

        [Benchmark]
        public bool StartsWith() => new System.Span<T>(_array).StartsWith(new System.ReadOnlySpan<T>(_same).Slice(start: 0, length: Size / 2));
        
        [Benchmark]
        public bool EndsWith() => new System.Span<T>(_array).EndsWith(new System.ReadOnlySpan<T>(_same).Slice(start: Size / 2));

        [GlobalSetup(Targets = new [] { nameof(IndexOfValue), nameof(LastIndexOfValue), nameof(LastIndexOfAnyValues) })]
        public void SetupIndexOf()
        {
            _notDefaultValue = ValuesGenerator.GetNonDefaultValue<T>();
            _emptyWithSingleValue = new T[Size];
            _emptyWithSingleValue[Size / 2] = _notDefaultValue;
        }

        [Benchmark]
        public int IndexOfValue() => new System.Span<T>(_emptyWithSingleValue).IndexOf(_notDefaultValue);
        
        [Benchmark]
        public int LastIndexOfValue() => new System.Span<T>(_emptyWithSingleValue).LastIndexOf(_notDefaultValue);
        
        [Benchmark]
        public int LastIndexOfAnyValues() => new System.Span<T>(_emptyWithSingleValue).LastIndexOfAny(_notDefaultValue, _notDefaultValue);
        
        [GlobalSetup(Target = nameof(BinarySearch))]
        public void SetupBinarySearch()
        {
            _notDefaultValue = ValuesGenerator.GetNonDefaultValue<T>();
            _emptyWithSingleValue = new T[Size];
            _emptyWithSingleValue[Size - 1] = _notDefaultValue;
        }
        
        [Benchmark]
        public int BinarySearch() => new System.Span<T>(_emptyWithSingleValue).BinarySearch(_notDefaultValue);

        [Benchmark(OperationsPerInvoke = 16)]
        public void GetPinnableReference()
        {
            var span = new System.Span<T>(_array);

            Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference());
            Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference());
            Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference());
            Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference()); Consume(span.GetPinnableReference());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Consume(in T _) { }
    }
}