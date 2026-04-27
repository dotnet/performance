// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;
using System;
using System.Runtime.CompilerServices;

namespace Struct
{
    [BenchmarkCategory(Categories.Runtime, Categories.JIT)]
    public class SpanWrapper
    {
        private int[] _array;

        [GlobalSetup]
        public void Setup()
        {
            _array = Enumerable.Range(0, 10000).ToArray();
        }

        [Benchmark]
        public int BaselineSum()
        {
            return SumSpan(_array);
        }

        [Benchmark]
        public int WrapperSum()
        {
            return SumSpanWrapper(new SpanWrapper<int> { Span = _array });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumSpan(ReadOnlySpan<int> span)
        {
            int sum = 0;
            foreach (int val in span)
                sum += val;

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumSpanWrapper(SpanWrapper<int> spanWrapper)
        {
            int sum = 0;
            foreach (int val in spanWrapper)
                sum += val;

            return sum;
        }
    }

    public ref struct SpanWrapper<T>
    {
        public ReadOnlySpan<T> Span;
        public ReadOnlySpan<T>.Enumerator GetEnumerator() => Span.GetEnumerator();
    }
}
