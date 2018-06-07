// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

// Performance tests for optimizations related to EqualityComparer<T>.Default

namespace Devirtualization
{
    public class EqualityComparerFixture<T> where T : IEquatable<T>
    {
        IEqualityComparer<T> comparer;

        public EqualityComparerFixture(IEqualityComparer<T> customComparer = null)
        {
            comparer = customComparer ?? EqualityComparer<T>.Default;
        }

        // Baseline method showing unoptimized performance
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public bool CompareNoOpt(ref T a, ref T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

        // The code this method invokes should be well-optimized
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Compare(ref T a, ref T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

        // This models how Dictionary uses a comparer. We're not
        // yet able to optimize such cases.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CompareCached(ref T a, ref T b)
        {
            return comparer.Equals(a, b);
        }

        private static IEqualityComparer<T> Wrapped()
        {
            return EqualityComparer<T>.Default;
        }

        // We would need enhancements to late devirtualization
        // to optimize this case.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CompareWrapped(ref T x, ref T y)
        {
            return Wrapped().Equals(x, y);
        }
    }

    [BenchmarkCategory(Categories.CoreCLR)]
    public class EqualityComparer
    {
        public enum E
        {
            RED = 1,
            BLUE = 2
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetInputData))]
        public bool ValueTupleCompareNoOpt(EqualityComparerFixture<ValueTuple<byte, E, int>> valueTupleFixture, ref ValueTuple<byte, E, int> v0)
            => valueTupleFixture.CompareNoOpt(ref v0, ref v0);

        [Benchmark]
        [ArgumentsSource(nameof(GetInputData))]
        public bool ValueTupleCompare(EqualityComparerFixture<ValueTuple<byte, E, int>> valueTupleFixture, ref ValueTuple<byte, E, int> v0)
            => valueTupleFixture.Compare(ref v0, ref v0);

        [Benchmark]
        [ArgumentsSource(nameof(GetInputData))]
        public bool ValueTupleCompareCached(EqualityComparerFixture<ValueTuple<byte, E, int>> valueTupleFixture, ref ValueTuple<byte, E, int> v0)
            => valueTupleFixture.CompareCached(ref v0, ref v0);

        [Benchmark]
        [ArgumentsSource(nameof(GetInputData))]
        public bool ValueTupleCompareWrapped(EqualityComparerFixture<ValueTuple<byte, E, int>> valueTupleFixture, ref ValueTuple<byte, E, int> v0)
            => valueTupleFixture.CompareWrapped(ref v0, ref v0);

        public IEnumerable<object[]> GetInputData()
        {
            yield return new object[]
            {
                new EqualityComparerFixture<ValueTuple<byte, E, int>>(),
                new ValueTuple<byte, E, int>(3, E.RED, 11)
            };
        }
    }
}
