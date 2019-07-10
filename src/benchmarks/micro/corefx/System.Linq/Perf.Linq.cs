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

        private readonly int[] _intArrayOfTenElements = Enumerable.Repeat(1, 10).ToArray();

        // used by benchmarks that have no special case per collection type
        public IEnumerable<object> IEnumerableArgument()
        {
            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
        }

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

        // .Where.First has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/First.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereFirst_LastElementMatches(LinqTestData collection) => collection.Collection.Where(x => x >= DefaultSize - 1).First();

        // .Where.Last has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Last.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereLast_LastElementMatches(LinqTestData collection) => collection.Collection.Where(x => x >= DefaultSize - 1).Last();

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

        public IEnumerable<object> LastPredicateArguments()
        {
            // .Last(predicate) has 3 code paths: OrderedEnumerable, IList and IEnumerable
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Last.cs

            yield return new LinqTestData(_arrayOf100Integers.OrderBy(x => x)); // .OrderBy returns IOrderedEnumerable (OrderedEnumerable is internal)
            yield return new LinqTestData(new IListWrapper<int>(_arrayOf100Integers));
            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
        }

        [Benchmark]
        [ArgumentsSource(nameof(LastPredicateArguments))]
        public int LastWithPredicate_FirstElementMatches(LinqTestData collection) => collection.Collection.Last(x => x >= 0);

        // FirstOrDefault runs the same code as First, except that it does not throw. I don't think that benchmarking it adds any value so I've removed it.
        // https://github.com/dotnet/corefx/blob/aef8ed681c53f0e04733878e240c072036dd6679/src/System.Linq/src/System/Linq/First.cs#L11-L37
        // The same goes for LastOrDefault
        // https://github.com/dotnet/corefx/blob/aef8ed681c53f0e04733878e240c072036dd6679/src/System.Linq/src/System/Linq/Last.cs#L11-L37

        // .Where.Any has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/AnyAll.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public bool WhereAny_LastElementMatches(LinqTestData collection) => collection.Collection.Where(x => x >= DefaultSize - 1).Any();

        // .Any has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/AnyAll.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public bool AnyWithPredicate_LastElementMatches(LinqTestData collection) => collection.Collection.Any(x => x >= DefaultSize - 1);

        // .Where.Single has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Single.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereSingle_LastElementMatches(LinqTestData collection) => collection.Collection.Where(x => x >= DefaultSize - 1).Single();

        // .Where.SingleOrDefault has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Single.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereSingleOrDefault_LastElementMatches(LinqTestData collection) => collection.Collection.Where(x => x >= DefaultSize - 1).SingleOrDefault();

        // .Single has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Single.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public int SingleWithPredicate_LastElementMatches(LinqTestData collection) => collection.Collection.Single(x => x >= DefaultSize - 1);

        // .Single has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Single.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public int SingleOrDefaultWithPredicate_LastElementMatches(LinqTestData collection) => collection.Collection.SingleOrDefault(x => x >= DefaultSize - 1);

        private readonly ChildClass[] _childClassArrayOfHundredElements = Enumerable.Repeat(new ChildClass() { Value = 1, ChildValue = 2 }, 100).ToArray();

        // Cast has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Cast.cs
        [Benchmark]
        public void Cast_ToBaseClass() => _childClassArrayOfHundredElements.Cast<BaseClass>().Consume(_consumer);

        [Benchmark]
        public void Cast_SameType() => _childClassArrayOfHundredElements.Cast<ChildClass>().Consume(_consumer);

        public IEnumerable<object> OrderByArguments()
        {
            int[] notSortedArray = ValuesGenerator.ArrayOfUniqueValues<int>(100);

            // .OrderBy has no special treatment, but we want to test already sorted collection and not sorted collection
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/OrderBy.cs

            yield return new LinqTestData(notSortedArray.OrderBy(x => x)); // sorted input scenario
            yield return new LinqTestData(new EnumerableWrapper<int>(notSortedArray));
        }

        [Benchmark]
        [ArgumentsSource(nameof(OrderByArguments))]
        public void OrderBy(LinqTestData collection) => collection.Collection.OrderBy(o => o).Consume(_consumer);

        [Benchmark]
        [ArgumentsSource(nameof(OrderByArguments))]
        public void OrderByDescending(LinqTestData collection) => collection.Collection.OrderByDescending(o => o).Consume(_consumer);

        [Benchmark]
        [ArgumentsSource(nameof(OrderByArguments))]
        public void OrderByThenBy(LinqTestData collection) => collection.Collection.OrderBy(o => o).ThenBy(o => -o).Consume(_consumer);

        [Benchmark]
        [Arguments(DefaultSize)]
        public void Range(int size) => Enumerable.Range(0, size).Consume(_consumer);

        [Benchmark]
        [Arguments(DefaultSize)]
        public void Repeat(int size) => Enumerable.Repeat(0, size).Consume(_consumer);

        // .Reverse has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Reverse.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Reverse(LinqTestData collection) => collection.Collection.Reverse().Consume(_consumer);

        // .Skip has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Skip.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Skip_One(LinqTestData collection) => collection.Collection.Skip(1).Consume(_consumer);

        // .Take has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Take.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Take_All(LinqTestData collection) => collection.Collection.Take(DefaultSize - 1).Consume(_consumer);

#if !NETFRAMEWORK
        public IEnumerable<object> TakeLastArguments()
        {
            // .TakeLast has 2 code paths: List and IEnumerable
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Take.SpeedOpt.cs

            yield return new LinqTestData(new List<int>(_arrayOf100Integers));
            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
        }

        [Benchmark]
        [ArgumentsSource(nameof(TakeLastArguments))]
        public void TakeLastHalf(LinqTestData collection) => collection.Collection.TakeLast(DefaultSize / 2).Consume(_consumer);
#endif

        // .Skip has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Skip.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void SkipHalfTakeHalf(LinqTestData collection) => collection.Collection.Skip(DefaultSize / 2).Take(DefaultSize / 2).Consume(_consumer);

        public IEnumerable<object> ToArrayArguments()
        {
            // .ToArray has two code paths: ICollection and IEnumerable
            // https://github.com/dotnet/corefx/blob/a10890f4ffe0fadf090c922578ba0e606ebdd16c/src/Common/src/System/Collections/Generic/EnumerableHelpers.Linq.cs#L93

            yield return new LinqTestData(_arrayOf100Integers);
            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
        }

        [Benchmark]
        [ArgumentsSource(nameof(ToArrayArguments))]
        public int[] ToArray(LinqTestData collection) => collection.Collection.ToArray();

        public IEnumerable<object> SelectToArrayArguments()
        {
            // .Select.ToArray has 5 code paths: SelectEnumerableIterator.ToArray, SelectArrayIterator.ToArray, SelectRangeIterator.ToArray, SelectListIterator.ToArray, SelectIListIterator.ToArray
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/Select.SpeedOpt.cs

            yield return new LinqTestData(new EnumerableWrapper<int>(_arrayOf100Integers));
            yield return new LinqTestData(_arrayOf100Integers);
            yield return new LinqTestData(Enumerable.Range(0, DefaultSize));
            yield return new LinqTestData(new List<int>(_arrayOf100Integers));
            yield return new LinqTestData(new IListWrapper<int>(_arrayOf100Integers));
        }

        [Benchmark]
        [ArgumentsSource(nameof(SelectToArrayArguments))]
        public int[] SelectToArray(LinqTestData collection) => collection.Collection.Select(o => o + 1).ToArray();

        // ToList has same 2 code paths as ToArray
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/ToCollection.cs#L30
        // https://github.com/dotnet/coreclr/blob/d61a380bbfde580986f416d8bf3e687104cd5701/src/System.Private.CoreLib/shared/System/Collections/Generic/List.cs#L61
        [Benchmark]
        [ArgumentsSource(nameof(ToArrayArguments))]
        public List<int> ToList(LinqTestData collection) => collection.Collection.ToList();

        // Select.ToList has same 5 code paths as Select.ToArray
        [Benchmark]
        [ArgumentsSource(nameof(SelectToArrayArguments))]
        public List<int> SelectToList(LinqTestData collection) => collection.Collection.Select(o => o + 1).ToList();

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
        public void Concat()
        {
            IEnumerable<int> result = _range0to10;
            for (int i = 0; i < 1000; i++)
            {
                result = result.Concat(_range0to10);
            }
            result.Consume(_consumer);
        }
    }
}