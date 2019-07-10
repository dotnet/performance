// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;
using static System.Linq.Tests.Perf_LinqTestBase;

namespace System.Linq.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.LINQ)]
    public class Perf_Linq
    {
        private const int DefaultSize = 100;
        private const int DefaulIterationCount = 1000;

        private readonly Consumer _consumer = new Consumer();
        private readonly int[] _arrayOf100Integers = Enumerable.Range(0, DefaultSize).ToArray();
        private readonly IEnumerable<int> _range0to10 = Enumerable.Range(0, 10);
        private readonly IEnumerable<int> _tenMillionToZero = Enumerable.Range(0, 10_000_000).Reverse();

        public static IEnumerable<object[]> IterationSizeWrapperData()
        {
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.NoWrap };
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.IEnumerable };
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.IReadOnlyCollection };
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.ICollection };
        }

        public static IEnumerable<object[]> IterationSizeReducedWrapperData()
        {
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.NoWrap };
            yield return new object[] { DefaultSize, DefaulIterationCount, Perf_LinqTestBase.WrapperType.IEnumerable };
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

        public IEnumerable<object> SelectArguments()
        {
            // .Select has 4 code paths: SelectEnumerableIterator, SelectArrayIterator, SelectListIterator, SelectIListIterator
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Select.cs

            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
            yield return new LinqTestData(_arrayOf100Integers);
            yield return new LinqTestData(new List<int>(_arrayOf100Integers));
            yield return new LinqTestData(new IListWrapper<int>(_arrayOf100Integers));
        }

        [Benchmark]
        [ArgumentsSource(nameof(SelectArguments))]
        public void Select(LinqTestData collection) => collection.Collection.Select(o => o + 1).Consume(_consumer);

        public IEnumerable<object> WhereArguments()
        {
            // .Where has 3 code paths: WhereEnumerableIterator, WhereArrayIterator, WhereListIterator
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Where.cs

            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
            yield return new LinqTestData(_arrayOf100Integers);
            yield return new LinqTestData(new List<int>(_arrayOf100Integers));
        }

        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public void Where(LinqTestData collection) => collection.Collection.Where(o => o >= 0).Consume(_consumer);

        // .Where.Select has 3 code paths: WhereSelectEnumerableIterator, WhereSelectArrayIterator, WhereSelectListIterator, exactly as .Where
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Where.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public void WhereSelect(LinqTestData collection) => collection.Collection.Where(o => o >= 0).Select(o => o + 1).Consume(_consumer);

        // .Where.First has no special treatment, the code execution paths are base on WhereIterator
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/First.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereFirst_LastElementMatches(LinqTestData collection) => collection.Collection.Where(x => x >= DefaultSize - 1).First();

        public IEnumerable<object> FirstPredicateArguments()
        {
            // .First(predicate) has 2 code paths: OrderedEnumerable and IEnumerable
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/First.cs

            yield return new LinqTestData(_arrayOf100Integers.OrderBy(x => x)); // .OrderBy returns IOrderedEnumerable (OrderedEnumerable is internal)
            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
        }

        [Benchmark]
        [ArgumentsSource(nameof(FirstPredicateArguments))]
        public int FirstWithPredicate_LastElementMatches(LinqTestData collection) => collection.Collection.First(x => x >= DefaultSize - 1);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int WhereFirstOrDefault_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Where(x => x >= size - 1).FirstOrDefault();

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int FirstOrDefaultWithPredicate_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).FirstOrDefault(x => x >= size - 1);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public bool WhereAny_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Where(x => x >= size - 1).Any();

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public bool AnyWithPredicate_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Any(x => x >= size - 1);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int WhereSingle_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Where(x => x >= size - 1).Single();

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int SingleWithPredicate_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Single(x => x >= size - 1);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int WhereSingleOrDefault_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Where(x => x >= size - 1).SingleOrDefault();

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int SingleOrDefaultWithPredicate_LastElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).SingleOrDefault(x => x >= size - 1);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int WhereLast(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Where(x => x % 2 == 0).Last();

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int LastWithPredicate(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Last(x => x % 2 == 0);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int WhereLastOrDefault(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).Where(x => x % 2 == 0).LastOrDefault();

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int LastOrDefaultWithPredicate(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType).LastOrDefault(x => x % 2 == 0);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))] // for some reason the size and iteration arguments are ignored for this benchmark
        public void Cast_ToBaseClass(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<ChildClass> source = Perf_LinqTestBase.Wrap(_childClassArrayOfTenElements, wrapType);

            source.Cast<BaseClass>().Consume(_consumer);
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))] // for some reason the size and iteration arguments are ignored for this benchmark
        public void Cast_SameType(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_intArrayOfTenElements, wrapType);

            source.Cast<int>().Consume(_consumer);
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void OrderBy(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.OrderBy(o => -o), _consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void OrderByDescending(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.OrderByDescending(o => -o), _consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void OrderByThenBy(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.OrderBy(o => -o).ThenBy(o => o), _consumer);

        [Benchmark]
        [Arguments(DefaultSize, DefaulIterationCount)]
        public void Range(int size, int iteration)
            => Enumerable.Range(0, size).Consume(_consumer);

        [Benchmark]
        [Arguments(DefaultSize, DefaulIterationCount)]
        public void Repeat(int size, int iteration)
            => Enumerable.Repeat(0, size).Consume(_consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void Reverse(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.Reverse(), _consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void Skip(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.Skip(1), _consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void Take(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.Take(size - 1), _consumer);

#if !NETFRAMEWORK
        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeReducedWrapperData))]
        public void TakeLastOne(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.TakeLast(1), _consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeReducedWrapperData))]
        public void TakeLastHalf(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.TakeLast(size / 2), _consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeReducedWrapperData))]
        public void TakeLastFull(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.TakeLast(size - 1), _consumer);
#endif

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public void SkipTake(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
            => Perf_LinqTestBase.Measure(_sizeToPreallocatedArray[size], wrapType, col => col.Skip(1).Take(size - 2), _consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] ToArray(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            return source.ToArray();
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public int[] SelectToArray(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            return source.Select(i => i).ToArray();
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public List<int> ToList(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            return source.ToList();
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public List<int> SelectToList(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            return source.Select(i => i).ToList();
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public Dictionary<int, int> ToDictionary(int size, int iteration, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            return source.ToDictionary(key => key);
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public bool Contains_ElementNotFound(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            return source.Contains(size + 1);
        }

        [Benchmark]
        [ArgumentsSource(nameof(IterationSizeWrapperData))]
        public bool Contains_FirstElementMatches(int size, int iterationCount, Perf_LinqTestBase.WrapperType wrapType)
        {
            IEnumerable<int> source = Perf_LinqTestBase.Wrap(_sizeToPreallocatedArray[size], wrapType);

            return source.Contains(0);
        }

        [Benchmark]
        public int Concat()
        {
            IEnumerable<int> result = _range0to10;
            for (int i = 0; i < 1000; i++)
            {
                result = result.Concat(_range0to10);
            }
            return result.Sum();
        }
    }
}