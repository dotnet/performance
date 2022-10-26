// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Runtime.Intrinsics.Tests
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(double))]
    [GenericTypeArguments(typeof(short))]
    [GenericTypeArguments(typeof(int))]
    [GenericTypeArguments(typeof(long))]
    [GenericTypeArguments(typeof(sbyte))]
    [GenericTypeArguments(typeof(float))]
    [GenericTypeArguments(typeof(ushort))]
    [GenericTypeArguments(typeof(uint))]
    [GenericTypeArguments(typeof(ulong))]
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Vector128T<T>
        where T : struct
    {
        private static readonly Vector128<T> Value1 = Vector128<T>.AllBitsSet;

        private static readonly Vector128<T> Value2 = Vector128<T>.AllBitsSet + Vector128<T>.AllBitsSet;

        private static readonly Vector128<T> Value3 = Vector128<T>.AllBitsSet + Vector128<T>.AllBitsSet + Vector128<T>.AllBitsSet;


        [Benchmark]
        public int CountBenchmark() => Vector128<T>.Count;

        [Benchmark]
        public Vector128<T> AllBitsSetBenchmark() => Vector128<T>.AllBitsSet;

        [Benchmark]
        public Vector128<T> ZeroBenchmark() => Vector128<T>.Zero;

        [Benchmark]
        public bool EqualsBenchmark() => Value1.Equals(Value2);

        [Benchmark]
        public int GetHashCodeBenchmark() => Value1.GetHashCode();

        [Benchmark]
        public Vector128<T> AddOperatorBenchmark() => Value1 + Value2;

        [Benchmark]
        public Vector128<T> BitwiseAndOperatorBenchmark() => Value1 & Value2;

        [Benchmark]
        public Vector128<T> BitwiseOrOperatorBenchmark() => Value1 | Value2;

        [Benchmark]
        public Vector128<T> DivisionOperatorBenchmark() => Value1 / Value2;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => Value1 == Value2;

        [Benchmark]
        public Vector128<T> ExclusiveOrOperatorBenchmark() => Value1 ^ Value2;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => Value1 != Value2;

        [Benchmark]
        public Vector128<T> MultiplyOperatorBenchmark() => Value1 * Value2;

        [Benchmark]
        public Vector128<T> OnesComplementOperatorBenchmark() => ~Value1;

        [Benchmark]
        public Vector128<T> SubtractionOperatorBenchmark() => Value1 - Value2;

        [Benchmark]
        public Vector128<T> UnaryNegateOperatorBenchmark() => -Value1;

        [Benchmark]
        public Vector128<T> AbsBenchmark() => Vector128.Abs(Value1);

        [Benchmark]
        public Vector128<T> AddBenchmark() => Vector128.Add(Value1, Value2);

        [Benchmark]
        public Vector128<T> AndNotBenchmark() => Vector128.AndNot(Value1, Value2);

        [Benchmark]
        public Vector128<T> BitwiseAndBenchmark() => Vector128.BitwiseAnd(Value1, Value2);

        [Benchmark]
        public Vector128<T> BitwiseOrBenchmark() => Vector128.BitwiseOr(Value1, Value2);

        [Benchmark]
        public Vector128<T> ConditionalSelectBenchmark() => Vector128.ConditionalSelect(Value1, Value2, Value3);

        [Benchmark]
        public Vector128<T> DivideBenchmark() => Vector128.Divide(Value1, Value2);

        [Benchmark]
        public T DotBenchmark() => Vector128.Dot(Value1, Value2);

        [Benchmark]
        public Vector128<T> EqualsStaticBenchmark() => Vector128.Equals(Value1, Value2);

        [Benchmark]
        public bool EqualsAllBenchmark() => Vector128.EqualsAll(Value1, Value2);

        [Benchmark]
        public bool EqualsAnyBenchmark() => Vector128.EqualsAny(Value1, Value2);

        [Benchmark]
        public Vector128<T> GreaterThanBenchmark() => Vector128.GreaterThan(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAllBenchmark() => Vector128.GreaterThanAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAnyBenchmark() => Vector128.GreaterThanAny(Value1, Value2);

        [Benchmark]
        public Vector128<T> GreaterThanOrEqualBenchmark() => Vector128.GreaterThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAllBenchmark() => Vector128.GreaterThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAnyBenchmark() => Vector128.GreaterThanOrEqualAny(Value1, Value2);

        [Benchmark]
        public Vector128<T> LessThanBenchmark() => Vector128.LessThan(Value1, Value2);

        [Benchmark]
        public bool LessThanAllBenchmark() => Vector128.LessThanAll(Value1, Value2);

        [Benchmark]
        public bool LessThanAnyBenchmark() => Vector128.LessThanAny(Value1, Value2);

        [Benchmark]
        public Vector128<T> LessThanOrEqualBenchmark() => Vector128.LessThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAllBenchmark() => Vector128.LessThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAnyBenchmark() => Vector128.LessThanOrEqualAny(Value1, Value2);

        [Benchmark]
        public Vector128<T> MaxBenchmark() => Vector128.Max(Value1, Value2);

        [Benchmark]
        public Vector128<T> MinBenchmark() => Vector128.Min(Value1, Value2);

        [Benchmark]
        public Vector128<T> MultiplyBenchmark() => Vector128.Multiply(Value1, Value2);

        [Benchmark]
        public Vector128<T> NegateBenchmark() => Vector128.Negate(Value1);

        [Benchmark]
        public Vector128<T> OnesComplementBenchmark() => Vector128.OnesComplement(Value1);

        [Benchmark]
        public Vector128<T> SquareRootBenchmark() => Vector128.Sqrt(Value1);

        [Benchmark]
        public Vector128<T> SubtractBenchmark() => Vector128.Subtract(Value1, Value2);

        [Benchmark]
        public T SumBenchmark() =>  Vector128.Sum(Value1);

        [Benchmark]
        public Vector128<T> XorBenchmark() => Vector128.Xor(Value1, Value2);
    }
}