// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace System.Linq.Tests
{
    public class Perf_Linq
    {
        private const int DefaultSize = 100;
        private const int DefaulIterationCount = 1000;

        public static IEnumerable<object[]> IterationSizeWrapperData()
        {
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.NoWrap };
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.IEnumerable };
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.IReadOnlyCollection };
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.ICollection };
        }

        public class BaseClass
        {
            public int Value;
        }
        private class ChildClass : BaseClass
        {
            public int ChildValue;
        }

        private readonly IReadOnlyDictionary<int, int[]> _sizeToPreallocatedArray = new Dictionary<int, int[]>
        {
            { DefaultSize, Enumerable.Range(0, DefaultSize).ToArray() }
        };

        private readonly ChildClass[] _childClassArrayOfTenElements = Enumerable.Repeat(new ChildClass() { Value = 1, ChildValue = 2 }, 10).ToArray();
        private readonly int[] _intArrayOfTenElements = Enumerable.Repeat(1, 10).ToArray();

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] Select(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Select(o => o + 1));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] SelectSelect(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Select(o => o + 1).Select(o => o - 1));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] Where(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Where(o => o >= 0));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] WhereWhere(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Where(o => o >= 0).Where(o => o >= -1));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void WhereSelect(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Where(o => o >= 0).Select(o => o + 1));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))] // for some reason the size and iteration arguments are ignored for this benchmark
        public BaseClass[] Cast_ToBaseClass(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            BaseClass[] baseClasses = default;
            IEnumerable<ChildClass> source = Perf_LinqTestBase.Wrap(_childClassArrayOfTenElements, wrapType);
                
            for (int i = 0; i < 5; i++)
                baseClasses = source.Cast<BaseClass>().ToArray();

            return baseClasses;
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))] // for some reason the size and iteration arguments are ignored for this benchmark
        public int[] Cast_SameType(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            int[] sameType = default;
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_intArrayOfTenElements, wrapType);

            for (int i = 0; i < 5; i++)
                sameType = source.Cast<int>().ToArray();

            return sameType;
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] OrderBy(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.OrderBy(o => -o));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] OrderByDescending(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.OrderByDescending(o => -o));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] OrderByThenBy(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.OrderBy(o => -o).ThenBy(o => o));

        [Benchmark]
        [Arguments(DefaultSize, DefaulIterationCount)]
        public int[] Range(int size, int iteration)
        {
            int[] array = default;

            for (int i = 0; i < iteration; i++)
                array = Enumerable.Range(0, size).ToArray();

            return array;
        }

        [Benchmark]
        [Arguments(DefaultSize, DefaulIterationCount)]
        public int[] Repeat(int size, int iteration)
        {
            int[] array = default;

            for (int i = 0; i < iteration; i++)
                array = Enumerable.Repeat(0, size).ToArray();

            return array;
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] Reverse(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Reverse());

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] Skip(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Skip(1));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] Take(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Take(size - 1));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] SkipTake(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType) 
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], iteration, wrapType, col => col.Skip(1).Take(size - 2));

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] ToArray(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            int[] array = default;
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            for (int i = 0; i < iteration; i++)
                array = source.ToArray();

            return array;
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public List<int> ToList(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            List<int> list = default;
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            for (int i = 0; i < iteration; i++)
                list = source.ToList();

            return list;
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public Dictionary<int, int> ToDictionary(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            Dictionary<int, int> dictionary = default;
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);
            int count = 0;

            for (int i = 0; i < iteration; i++)
                dictionary = source.ToDictionary(key => count++);

            return dictionary;
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public bool Contains_ElementNotFound(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
        {
            bool contains = default;
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            for (int i = 0; i < iterationCount; i++)
                contains = source.Contains(size + 1);

            return contains;
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public bool Contains_FirstElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
        {
            bool contains = default;
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            for (int i = 0; i < iterationCount; i++)
                contains = source.Contains(0);

            return contains;
        }
    }
}
