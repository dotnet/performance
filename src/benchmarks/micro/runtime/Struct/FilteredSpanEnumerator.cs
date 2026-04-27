// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Struct
{
    [BenchmarkCategory(Categories.Runtime, Categories.JIT)]
    public class FilteredSpanEnumerator
    {
        private int[] _array;
        private int[] _inds;

        [GlobalSetup]
        public void Setup()
        {
            _array = Enumerable.Range(0, 10000).ToArray();
            _inds = Enumerable.Range(0, 10000).ToArray();
        }

        [Benchmark]
        public int Sum()
        {
            int sum = 0;
            foreach (int s in new FilteredSpanEnumerator<int>(_array, _inds))
            {
                sum += s;
            }

            return sum;
        }
    }

    public ref struct FilteredSpanEnumerator<T>
    {
        private readonly ReadOnlySpan<T> arr;
        private readonly int[] inds;

        private T current;
        private int i;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FilteredSpanEnumerator(ReadOnlySpan<T> arr, int[] inds) {
            this.arr = arr;
            this.inds = inds;
            current = default;
            i = 0;
        }

        public T Current => current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() {
            if (i >= inds.Length)
                return false;

            current = arr[inds[i++]];
            return true;
        }

        public FilteredSpanEnumerator<T> GetEnumerator() => this;
    }
}
