// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Order;
using MicroBenchmarks;

namespace System.Collections
{
    [InvocationCount(InvocationsPerIteration)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public class SortInt32 : Sort<int>
    {
        private static readonly SpecificComparerClass _specificComparerClass = new SpecificComparerClass();
        private const int InvocationsPerIteration = 15000;
        public SortInt32() : base(InvocationsPerIteration) { }

//#if NETCOREAPP5_0
        [Benchmark]
        public void Span_ComparerClassSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(_specificComparerClass);

        [Benchmark]
        public void Span_ComparerStructSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(new SpecificComparerStruct());

        private sealed class SpecificComparerClass : IComparer<int>
        {
            public int Compare(int x, int y) => x.CompareTo(y);
        }

        private readonly struct SpecificComparerStruct : IComparer<int>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(int x, int y) => x.CompareTo(y);
        }
//#endif
    }

    [InvocationCount(InvocationsPerIteration)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public class SortIntStruct : Sort<IntStruct>
    {
        private static readonly SpecificComparerClass _specificComparerClass = new SpecificComparerClass();
        private const int InvocationsPerIteration = 15000;
        public SortIntStruct() : base(InvocationsPerIteration) { }

//#if NETCOREAPP5_0
        [Benchmark]
        public void Span_ComparerClassSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(_specificComparerClass);

        [Benchmark]
        public void Span_ComparerStructSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(new SpecificComparerStruct());

        private sealed class SpecificComparerClass : IComparer<IntStruct>
        {
            public int Compare(IntStruct x, IntStruct y) => x.CompareTo(y);
        }

        private readonly struct SpecificComparerStruct : IComparer<IntStruct>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(IntStruct x, IntStruct y) => x.CompareTo(y);
        }
//#endif
    }


    [InvocationCount(InvocationsPerIteration)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public class SortIntClass : Sort<IntClass>
    {
        private static readonly SpecificComparerClass _specificComparerClass = new SpecificComparerClass();
        private const int InvocationsPerIteration = 15000;
        public SortIntClass() : base(InvocationsPerIteration) { }

//#if NETCOREAPP5_0
        [Benchmark]
        public void Span_ComparerClassSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(_specificComparerClass);

        [Benchmark]
        public void Span_ComparerStructSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(new SpecificComparerStruct());

        private sealed class SpecificComparerClass : IComparer<IntClass>
        {
            public int Compare(IntClass x, IntClass y) => x.CompareTo(y);
        }

        private readonly struct SpecificComparerStruct : IComparer<IntClass>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(IntClass x, IntClass y) => x.CompareTo(y);
        }
//#endif
    }

    [InvocationCount(InvocationsPerIteration)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public class SortBigStruct : Sort<BigStruct>
    {
        private static readonly SpecificComparerClass _specificComparerClass = new SpecificComparerClass();
        private const int InvocationsPerIteration = 5000;
        public SortBigStruct() : base(InvocationsPerIteration) { }

//#if NETCOREAPP5_0
        [Benchmark]
        public void Span_ComparerClassSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(_specificComparerClass);

        [Benchmark]
        public void Span_ComparerStructSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(new SpecificComparerStruct());

        private sealed class SpecificComparerClass : IComparer<BigStruct>
        {
            public int Compare(BigStruct x, BigStruct y) => x.CompareTo(y);
        }

        private readonly struct SpecificComparerStruct : IComparer<BigStruct>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(BigStruct x, BigStruct y) => x.CompareTo(y);
        }
//#endif
    }

    [InvocationCount(InvocationsPerIteration)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public class SortString : Sort<string>
    {
        private static readonly SpecificComparerClass _specificComparerClass = new SpecificComparerClass();
        private const int InvocationsPerIteration = 1000;
        public SortString() : base(InvocationsPerIteration) { }

//#if NETCOREAPP5_0
        [Benchmark]
        public void Span_ComparerClassSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(_specificComparerClass);

        [Benchmark]
        public void Span_ComparerStructSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(new SpecificComparerStruct());

        [Benchmark]
        public void Span_ComparerStructCompareInfo() => _arrays[_iterationIndex++].AsSpan().Sort(CompareInfoComparerStruct.CreateForCurrentCulture());

        private sealed class SpecificComparerClass : IComparer<string>
        {
            public int Compare(string x, string y) => x.CompareTo(y);
        }

        private readonly struct SpecificComparerStruct : IComparer<string>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(string x, string y) => x.CompareTo(y);
        }

        private readonly struct CompareInfoComparerStruct : IComparer<string>
        {
            private readonly CompareInfo m_compareInfo;

            public CompareInfoComparerStruct(CompareInfo compareInfo) =>
                m_compareInfo = compareInfo;

            // Getting CurrentCulture.CompareInfo for each compare is slower,
            // so instead we get it once "caching it" and use it for default string sorting.
            public static CompareInfoComparerStruct CreateForCurrentCulture() =>
                new CompareInfoComparerStruct(CultureInfo.CurrentCulture.CompareInfo);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(string x, string y) => x.CompareTo(y);
        }
//#endif
    }

    [InvocationCount(InvocationsPerIteration)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public class SortFloat : Sort<float>
    {
        private static readonly SpecificComparerClass _specificComparerClass = new SpecificComparerClass();
        private const int InvocationsPerIteration = 5000;
        public SortFloat() : base(InvocationsPerIteration) { }

//#if NETCOREAPP5_0
        [Benchmark]
        public void Span_ComparerClassSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(_specificComparerClass);

        [Benchmark]
        public void Span_ComparerStructSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(new SpecificComparerStruct());

        private sealed class SpecificComparerClass : IComparer<float>
        {
            public int Compare(float x, float y) => x.CompareTo(y);
        }

        private readonly struct SpecificComparerStruct : IComparer<float>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(float x, float y) => x.CompareTo(y);
        }
//#endif
    }

    [InvocationCount(InvocationsPerIteration)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public class SortDouble : Sort<double>
    {
        private static readonly SpecificComparerClass _specificComparerClass = new SpecificComparerClass();
        private const int InvocationsPerIteration = 5000;
        public SortDouble() : base(InvocationsPerIteration) { }

//#if NETCOREAPP5_0
        [Benchmark]
        public void Span_ComparerClassSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(_specificComparerClass);

        [Benchmark]
        public void Span_ComparerStructSpecific() => _arrays[_iterationIndex++].AsSpan().Sort(new SpecificComparerStruct());

        private sealed class SpecificComparerClass : IComparer<double>
        {
            public int Compare(double x, double y) => x.CompareTo(y);
        }

        private readonly struct SpecificComparerStruct : IComparer<double>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(double x, double y) => x.CompareTo(y);
        }
//#endif
    }


    [Orderer(SummaryOrderPolicy.Method, MethodOrderPolicy.Alphabetical)]
    [BenchmarkCategory(Categories.Runtime, Categories.Collections, Categories.GenericCollections)]
    public abstract class Sort<T> where T : IComparable<T>
    {
        private static readonly ComparableComparerClass _comparableComparerClass = new ComparableComparerClass();
        private readonly int _invocationsPerIteration;

        public Sort(int invocationsPerIteration) => _invocationsPerIteration = invocationsPerIteration;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        protected int _iterationIndex = 0;
        protected T[] _values;
        protected T[][] _arrays;
        protected List<T>[] _lists;

        [GlobalSetup]
        public void Setup() => _values = GenerateValues();

        [IterationCleanup]
        public void CleanupIteration() => _iterationIndex = 0; // after every iteration end we set the index to 0

//        [IterationSetup(Targets = new []{ nameof(Array), nameof(Array_ComparerClassGeneric),
//            nameof(Array_ComparerStructGeneric), nameof(Array_Comparison),
////#if NETCOREAPP5_0
//            nameof(Span), nameof(Span_ComparerClassGeneric),
//            nameof(Span_ComparerStructGeneric), nameof(Span_Comparison)})
////#endif
//            ]
        // Can't do iteration setup with targets in a clean way, setup is fast enough compared to sort not a big concern
        [IterationSetup()]
        public virtual void SetupArrayIteration() => Utils.FillArrays(ref _arrays, _invocationsPerIteration, _values);

        [Benchmark]
        public void Array() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size);

        [Benchmark]
        public void Array_ComparerClassGeneric() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size, _comparableComparerClass);

        [Benchmark]
        public void Array_ComparerStructGeneric() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size, new ComparableComparerStruct());

        [Benchmark]
        public void Array_Comparison() => System.Array.Sort(_arrays[_iterationIndex++], (x, y) => x.CompareTo(y));
//#if NETCOREAPP5_0
        [Benchmark]
        public void Span() => _arrays[_iterationIndex++].AsSpan().Sort();

        [Benchmark]
        public void Span_Comparison() => _arrays[_iterationIndex++].AsSpan().Sort((x, y) => x.CompareTo(y));

        [Benchmark]
        public void Span_ComparerClassGeneric() => _arrays[_iterationIndex++].AsSpan().Sort(_comparableComparerClass);

        [Benchmark]
        public void Span_ComparerStructGeneric() => _arrays[_iterationIndex++].AsSpan().Sort(new ComparableComparerStruct());
//#endif

        [IterationSetup(Target = nameof(List))]
        public void SetupListIteration() => Utils.ClearAndFillCollections(ref _lists, _invocationsPerIteration, _values);

        [Benchmark]
        public void List() => _lists[_iterationIndex++].Sort();

        [BenchmarkCategory(Categories.LINQ)]
        [Benchmark]
        public int LinqQuery()
        {
            int count = 0;
            foreach (var _ in (from value in _values orderby value ascending select value))
                count++;
            return count;
        }

        [BenchmarkCategory(Categories.LINQ)]
        [Benchmark]
        public int LinqOrderByExtension()
        {
            int count = 0;
            foreach (var _ in _values.OrderBy(value => value)) // we can't use .Count here because it has been optimized for icollection.OrderBy().Count()
                count++;
            return count;
        }

        private T[] GenerateValues()
        {
            if (typeof(T) == typeof(IntStruct))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new IntStruct(v)).ToArray();
            }
            else if (typeof(T) == typeof(IntClass))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new IntClass(v)).ToArray();
            }
            else if (typeof(T) == typeof(BigStruct))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new BigStruct(v)).ToArray();
            }
            else
            {
                return ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            }
        }

        private sealed class ComparableComparerClass : IComparer<T>
        {
            public int Compare(T x, T y) => x.CompareTo(y);
        }

        private readonly struct ComparableComparerStruct : IComparer<T>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(T x, T y) => x.CompareTo(y);
        }
    }
}
