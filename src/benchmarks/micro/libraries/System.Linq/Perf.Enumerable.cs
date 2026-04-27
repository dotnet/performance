// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MicroBenchmarks;

namespace System.Linq.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.LINQ)]
    public class Perf_Enumerable
    {
        private readonly Consumer _consumer = new Consumer();

        // used by benchmarks that have no special handling per collection type
        public IEnumerable<object> IEnumerableArgument()
        {
            yield return LinqTestData.IEnumerable;
        }

        public IEnumerable<object> SelectArguments()
        {
            // Select() has 4 code paths: SelectEnumerableIterator, SelectArrayIterator, SelectListIterator, SelectIListIterator
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Select.cs

            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.Array;
            yield return LinqTestData.List;
            yield return LinqTestData.IList;
        }

        [Benchmark]
        [ArgumentsSource(nameof(SelectArguments))]
        public void Select(LinqTestData input) => input.Collection.Select(i => i + 1).Consume(_consumer);

        public IEnumerable<object> WhereArguments()
        {
            // Where() has 3 code paths: WhereEnumerableIterator, WhereArrayIterator, WhereListIterator
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Where.cs

            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.Array;
            yield return LinqTestData.List;
        }

        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public void Where(LinqTestData input) => input.Collection.Where(i => i >= 0).Consume(_consumer);

        // Where().Select() has 3 code paths: WhereSelectEnumerableIterator, WhereSelectArrayIterator, WhereSelectListIterator, exactly as Where
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Where.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public void WhereSelect(LinqTestData input) => input.Collection.Where(i => i >= 0).Select(i => i + 1).Consume(_consumer);

        // Where().First() has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/First.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereFirst_LastElementMatches(LinqTestData input) => input.Collection.Where(i => i >= LinqTestData.Size - 1).First();

        // Where().Last() has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Last.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereLast_LastElementMatches(LinqTestData input) => input.Collection.Where(i => i >= LinqTestData.Size - 1).Last();

        public IEnumerable<object> FirstPredicateArguments()
        {
            // First(predicate) has 4 code paths: OrderedEnumerable, Array, List, and IEnumerable
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/First.cs

            yield return LinqTestData.IOrderedEnumerable;
            yield return LinqTestData.Array;
            yield return LinqTestData.List;
            yield return LinqTestData.IEnumerable;
        }

        [Benchmark]
        [ArgumentsSource(nameof(FirstPredicateArguments))]
        public int FirstWithPredicate_LastElementMatches(LinqTestData input) => input.Collection.First(i => i >= LinqTestData.Size - 1);

        public IEnumerable<object> LastPredicateArguments()
        {
            // Last(predicate) has 3 code paths: OrderedEnumerable, IList and IEnumerable
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Last.cs

            yield return LinqTestData.IOrderedEnumerable;
            yield return LinqTestData.IList;
            yield return LinqTestData.IEnumerable;
        }

        [Benchmark]
        [ArgumentsSource(nameof(LastPredicateArguments))]
        public int LastWithPredicate_FirstElementMatches(LinqTestData input) => input.Collection.Last(i => i >= 0);

        // FirstOrDefault() runs the same code as First, except that it does not throw. Benchmarking it does not add any value so it go removed.
        // https://github.com/dotnet/corefx/blob/aef8ed681c53f0e04733878e240c072036dd6679/src/System.Linq/src/System/Linq/First.cs#L11-L37
        // The same goes for LastOrDefault()
        // https://github.com/dotnet/corefx/blob/aef8ed681c53f0e04733878e240c072036dd6679/src/System.Linq/src/System/Linq/Last.cs#L11-L37

        // Where().Any() has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/AnyAll.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public bool WhereAny_LastElementMatches(LinqTestData input) => input.Collection.Where(i => i >= LinqTestData.Size - 1).Any();

        // Any uses TryGetFirst internally.
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/First.cs
        [Benchmark]
        [ArgumentsSource(nameof(FirstPredicateArguments))]
        public bool AnyWithPredicate_LastElementMatches(LinqTestData input) => input.Collection.Any(i => i >= LinqTestData.Size - 1);

        // All() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/AnyAll.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public bool All_AllElementsMatch(LinqTestData input) => input.Collection.All(i => i >= 0);

        // Where().Single() has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Single.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereSingle_LastElementMatches(LinqTestData input) => input.Collection.Where(i => i >= LinqTestData.Size - 1).Single();

        // Where().SingleOrDefault() has no special treatment, the code execution paths are based on WhereIterators
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Single.cs
        [Benchmark]
        [ArgumentsSource(nameof(WhereArguments))]
        public int WhereSingleOrDefault_LastElementMatches(LinqTestData input) => input.Collection.Where(i => i >= LinqTestData.Size - 1).SingleOrDefault();

        public IEnumerable<object> SinglePredicateArguments()
        {
            // Single(predicate) has 3 code paths: Array, List, and IEnumerable
            // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/First.cs

            yield return LinqTestData.Array;
            yield return LinqTestData.List;
            yield return LinqTestData.IEnumerable;
        }

        [Benchmark]
        [ArgumentsSource(nameof(SinglePredicateArguments))]
        public int SingleWithPredicate_LastElementMatches(LinqTestData input) => input.Collection.Single(i => i >= LinqTestData.Size - 1);

        [Benchmark]
        [ArgumentsSource(nameof(SinglePredicateArguments))]
        public int SingleWithPredicate_FirstElementMatches(LinqTestData input) => input.Collection.Single(i => i <= 0);

        // SingleOrDefault() runs the same code as Single, except that it does not throw. Benchmarking it does not add any value so it go removed.
        // https://github.com/dotnet/corefx/blob/master/src/System.Linq/src/System/Linq/First.cs

        // Cast has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Cast.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void CastToBaseClass(LinqTestData input) => input.Collection.Cast<object>().Consume(_consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void CastToSameType(LinqTestData input) => input.Collection.Cast<int>().Consume(_consumer);

        // OrderBy() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/OrderBy.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void OrderBy(LinqTestData input) => input.Collection.OrderBy(i => i).Consume(_consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        [MemoryRandomization]
        public void OrderByDescending(LinqTestData input) => input.Collection.OrderByDescending(i => i).Consume(_consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void OrderByThenBy(LinqTestData input) => input.Collection.OrderBy(i => i).ThenBy(i => -i).Consume(_consumer);

        [Benchmark]
        public void Range() => Enumerable.Range(0, LinqTestData.Size).Consume(_consumer);

        [Benchmark]
        public void Repeat() => Enumerable.Repeat(0, LinqTestData.Size).Consume(_consumer);

        // Reverse() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Reverse.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Reverse(LinqTestData input) => input.Collection.Reverse().Consume(_consumer);

        // Skip() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Skip.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Skip_One(LinqTestData input) => input.Collection.Skip(1).Consume(_consumer);

        // Take() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Take.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Take_All(LinqTestData input) => input.Collection.Take(LinqTestData.Size - 1).Consume(_consumer);

#if !NETFRAMEWORK
        public IEnumerable<object> TakeLastArguments()
        {
            // TakeLast has 2 code paths: List and IEnumerable
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Take.SpeedOpt.cs

            yield return LinqTestData.List;
            yield return LinqTestData.IEnumerable;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TakeLastArguments))]
        public void TakeLastHalf(LinqTestData input) => input.Collection.TakeLast(LinqTestData.Size / 2).Consume(_consumer);
#endif

        // Skip() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Skip.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void SkipHalfTakeHalf(LinqTestData input) => input.Collection.Skip(LinqTestData.Size / 2).Take(LinqTestData.Size / 2).Consume(_consumer);

        public IEnumerable<object> ToArrayArguments()
        {
            // ToArray() has two code paths: ICollection and IEnumerable
            // https://github.com/dotnet/corefx/blob/a10890f4ffe0fadf090c922578ba0e606ebdd16c/src/Common/src/System/Collections/Generic/EnumerableHelpers.Linq.cs#L93

            yield return LinqTestData.ICollection;
            yield return LinqTestData.IEnumerable;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ToArrayArguments))]
        public int[] ToArray(LinqTestData input) => input.Collection.ToArray();

        public IEnumerable<object> SelectToArrayArguments()
        {
            // Select().ToArray() has 5 code paths: SelectEnumerableIterator.ToArray, SelectArrayIterator.ToArray, SelectRangeIterator.ToArray, SelectListIterator.ToArray, SelectIListIterator.ToArray
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Select.SpeedOpt.cs

            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.Array;
            yield return LinqTestData.Range;
            yield return LinqTestData.List;
            yield return LinqTestData.IList;
        }

        [Benchmark]
        [ArgumentsSource(nameof(SelectToArrayArguments))]
        public int[] SelectToArray(LinqTestData input) => input.Collection.Select(i => i + 1).ToArray();

        // ToList() has same 2 code paths as ToArray
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/ToCollection.cs#L30
        // https://github.com/dotnet/coreclr/blob/d61a380bbfde580986f416d8bf3e687104cd5701/src/System.Private.CoreLib/shared/System/Collections/Generic/List.cs#L61
        [Benchmark]
        [ArgumentsSource(nameof(ToArrayArguments))]
        public List<int> ToList(LinqTestData input) => input.Collection.ToList();

        // Select().ToList() has same 5 code paths as Select.ToArray
        [Benchmark]
        [ArgumentsSource(nameof(SelectToArrayArguments))]
        public List<int> SelectToList(LinqTestData input) => input.Collection.Select(i => i + 1).ToList();

        public IEnumerable<object> ToDictionaryArguments()
        {
            // ToDictionary() has 3 code paths: Array, List and IEnumerable
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/ToCollection.cs#L36

            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.Array;
            yield return LinqTestData.List;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ToDictionaryArguments))]
        public Dictionary<int, int> ToDictionary(LinqTestData input) => input.Collection.ToDictionary(key => key);

        public IEnumerable<object> ContainsArguments()
        {
            // Contains() has 2 code paths: ICollection and IEnumerable
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Contains.cs

            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.ICollection;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ContainsArguments))]
        public bool Contains_ElementNotFound(LinqTestData input) => input.Collection.Contains(LinqTestData.Size + 1);

        // Concat() has two execution paths: ConcatIterator (a result of another Concatenation) and IEnumerable
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Concat.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Concat_Once(LinqTestData input) => input.Collection.Concat(input.Collection).Consume(_consumer);

        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Concat_TenTimes(LinqTestData input)
        {
            IEnumerable<int> result = input.Collection;
            for (int i = 0; i < 10; i++)
            {
                result = result.Concat(input.Collection); // test ConcatIterator execution path
            }
            result.Consume(_consumer);
        }

        // Sum() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Sum.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public int Sum(LinqTestData input) => input.Collection.Sum();

        // Min() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Min.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public int Min(LinqTestData input) => input.Collection.Min();

        // Max() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Max.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public int Max(LinqTestData input) => input.Collection.Max();

        // Average() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Average.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public double Average(LinqTestData input) => input.Collection.Average();

        public IEnumerable<object> CountArguments()
        {
            // Count() has 2 code paths: ICollection and IEnumerable
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Count.cs

            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.ICollection;
        }

        [Benchmark]
        [ArgumentsSource(nameof(CountArguments))]
        public int Count(LinqTestData input) => input.Collection.Count();

        // Aggregate(func) has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Aggregate.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public double Aggregate(LinqTestData input) => input.Collection.Aggregate((x, y) => x + y);

        // Aggregate(seed, func) has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Aggregate.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public double Aggregate_Seed(LinqTestData input) => input.Collection.Aggregate(0, (x, y) => x + y);

        // Distinct() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Distinct.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Distinct(LinqTestData input) => input.Collection.Distinct().Consume(_consumer);

        public IEnumerable<object> ElementAtArguments()
        {
            // ElementAt() has 2 code paths: IList and IEnumerable
            // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/ElementAt.cs

            yield return LinqTestData.IEnumerable;
            yield return LinqTestData.IList;
        }

        [Benchmark]
        [ArgumentsSource(nameof(ElementAtArguments))]
        public int ElementAt(LinqTestData input) => input.Collection.ElementAt(LinqTestData.Size / 2);

        // GroupBy(func) has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Grouping.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void GroupBy(LinqTestData input) => input.Collection.GroupBy(x => x % 10).Consume(_consumer);

        // Zip(func) has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Zip.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Zip(LinqTestData input) => input.Collection.Zip(input.Collection, (x, y) => x + y).Consume(_consumer);

        // Intersect() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Intersect.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        // Intersect() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Intersect.cs
        [MemoryRandomization]
        public void Intersect(LinqTestData input) => input.Collection.Intersect(input.Collection).Consume(_consumer);

        // Except() has no special treatment and it has a single execution path
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/Except.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Except(LinqTestData input) => input.Collection.Except(input.Collection).Consume(_consumer);

        [Benchmark]
        public void EmptyTakeSelectToArray() => Enumerable.Empty<int>().Take(10).Select(i => i).ToArray();

#if !NETFRAMEWORK // API Available in .NET Core 3.0+
        public IEnumerable<object[]> SequenceEqualArguments()
        {
            yield return new object[] { LinqTestData.Array, LinqTestData.Array };
            yield return new object[] { LinqTestData.IEnumerable, LinqTestData.IEnumerable };
        }

        [Benchmark]
        [ArgumentsSource(nameof(SequenceEqualArguments))]
        [MemoryRandomization]
        public bool SequenceEqual(LinqTestData input1, LinqTestData input2) => input1.Collection.SequenceEqual(input2.Collection);

        // Append() has two execution paths: AppendPrependIterator (a result of another Append or Prepend) and IEnumerable, this benchmark tests both
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/AppendPrepend.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Append(LinqTestData input)
        {
            IEnumerable<int> result = Enumerable.Empty<int>();
            foreach (int item in input.Collection)
            {
                result = result.Append(item);
            }
            result.Consume(_consumer);
        }

        // Prepend()has two execution paths: AppendPrependIterator (a result of another Append or Prepend) and IEnumerable, this benchmark tests both
        // https://github.com/dotnet/corefx/blob/dcf1c8f51bcdbd79e08cc672e327d50612690a25/src/System.Linq/src/System/Linq/AppendPrepend.cs
        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void Prepend(LinqTestData input)
        {
            IEnumerable<int> result = Enumerable.Empty<int>();
            foreach (int item in input.Collection)
            {
                result = result.Prepend(item);
            }
            result.Consume(_consumer);
        }

        [Benchmark]
        [ArgumentsSource(nameof(IEnumerableArgument))]
        public void AppendPrepend(LinqTestData input)
        {
            IEnumerable<int> result = Enumerable.Empty<int>();
            int index = 0;
            foreach (int item in input.Collection)
            {
                if (index % 2 == 0)
                {
                    result = result.Append(item);
                }
                else
                {
                    result = result.Prepend(item);
                }
                index++;
            }
            result.Consume(_consumer);
        }
#endif
    }
}