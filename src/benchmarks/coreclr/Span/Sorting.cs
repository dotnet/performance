using System;
using System.Collections;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace Span
{
    [BenchmarkCategory(Categories.CoreCLR)]
    [InvocationCount(InvocationsPerIteration)]
    public class Sorting
    {
        private const int InvocationsPerIteration = 1000;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private int _iterationIndex = 0;
        private int[] _values;
        
        private int[][] _arrays;
        
        [GlobalSetup]
        public void Setup() => _values = UniqueValuesGenerator.GenerateArray<int>(Size);
        
        [IterationSetup]
        public void SetupSpanIteration() => Utils.FillArrays(ref _arrays, InvocationsPerIteration, _values);
        
        [IterationCleanup]
        public void CleanupIteration() => _iterationIndex = 0; // after every iteration end we set the index to 0
        
        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public void QuickSortSpan() => TestQuickSortSpan(new Span<int>(_arrays[_iterationIndex++]));

        [BenchmarkCategory(Categories.Span)]
        [Benchmark]
        public void BubbleSortSpan() => TestBubbleSortSpan(new Span<int>(_arrays[_iterationIndex++]));

        [Benchmark]
        public void QuickSortArray() => TestQuickSortArray(_arrays[_iterationIndex++], 0, Size);

        [Benchmark]
        public void BubbleSortArray() => TestBubbleSortArray(_arrays[_iterationIndex++]);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestQuickSortArray(int[] data, int lo, int hi)
        {
            if (lo >= hi)
            {
                return;
            }

            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;)
            {
                while (i < j && data[i] <= pivot)
                {
                    ++i;
                }
                while (j > i && data[j] >= pivot)
                {
                    --j;
                }
                if (i < j)
                {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi)
            {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            TestQuickSortArray(data, lo, i - 1);
            TestQuickSortArray(data, i + 1, hi);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestQuickSortSpan(Span<int> data)
        {
            if (data.Length <= 1)
            {
                return;
            }

            int lo = 0;
            int hi = data.Length - 1;
            int i, j;
            int pivot, temp;
            for (i = lo, j = hi, pivot = data[hi]; i < j;)
            {
                while (i < j && data[i] <= pivot)
                {
                    ++i;
                }
                while (j > i && data[j] >= pivot)
                {
                    --j;
                }
                if (i < j)
                {
                    temp = data[i];
                    data[i] = data[j];
                    data[j] = temp;
                }
            }
            if (i != hi)
            {
                temp = data[i];
                data[i] = pivot;
                data[hi] = temp;
            }

            TestQuickSortSpan(data.Slice(0, i));
            TestQuickSortSpan(data.Slice(i + 1));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortArray(int[] data)
        {
            bool swap;
            int temp;
            int n = data.Length - 1;
            do
            {
                swap = false;
                for (int i = 0; i < n; i++)
                {
                    if (data[i] > data[i + 1])
                    {
                        temp = data[i];
                        data[i] = data[i + 1];
                        data[i + 1] = temp;
                        swap = true;
                    }
                }
                --n;
            }
            while (swap);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestBubbleSortSpan(Span<int> span)
        {
            bool swap;
            int temp;
            int n = span.Length - 1;
            do
            {
                swap = false;
                for (int i = 0; i < n; i++)
                {
                    if (span[i] > span[i + 1])
                    {
                        temp = span[i];
                        span[i] = span[i + 1];
                        span[i + 1] = temp;
                        swap = true;
                    }
                }
                --n;
            }
            while (swap);
        }
    }
}