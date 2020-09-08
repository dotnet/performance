// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    public class Perf_VectorOfByte : Perf_VectorOf<byte> { }

    public class Perf_VectorOfDouble : Perf_VectorOf<double> { }

    public class Perf_VectorOfInt16 : Perf_VectorOf<short> { }

    public class Perf_VectorOfInt32 : Perf_VectorOf<int> { }

    public class Perf_VectorOfInt64 : Perf_VectorOf<long> { }

    public class Perf_VectorOfSByte : Perf_VectorOf<sbyte> { }

    public class Perf_VectorOfSingle : Perf_VectorOf<float> { }

    public class Perf_VectorOfUInt16 : Perf_VectorOf<ushort> { }

    public class Perf_VectorOfUInt32 : Perf_VectorOf<uint> { }

    public class Perf_VectorOfUInt64 : Perf_VectorOf<ulong> { }

    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_VectorOf<T>
        where T : struct
    {
        private static readonly Vector<T> Value1 = Vector<T>.One;

        private static readonly Vector<T> Value2 = Vector<T>.One + Vector<T>.One;

        private static readonly Vector<T> Value3 = Vector<T>.One + Vector<T>.One + Vector<T>.One;

        [Benchmark]
        public int CountBenchmark() => Vector<T>.Count;

        [Benchmark]
        public Vector<T> OneBenchmark() => Vector<T>.One;

        [Benchmark]
        public Vector<T> ZeroBenchmark() => Vector<T>.Zero;

        [Benchmark]
        public bool EqualsBenchmark() => Value1.Equals(Value2);

        [Benchmark]
        public int GetHashCodeBenchmark() => Value1.GetHashCode();

        [Benchmark]
        public Vector<T> AddOperatorBenchmark() => Value1 + Value2;

        [Benchmark]
        public Vector<T> BitwiseAndOperatorBenchmark() => Value1 & Value2;

        [Benchmark]
        public Vector<T> BitwiseOrOperatorBenchmark() => Value1 | Value2;

        [Benchmark]
        public Vector<T> DivisionOperatorBenchmark() => Value1 / Value2;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => Value1 == Value2;

        [Benchmark]
        public Vector<T> ExclusiveOrOperatorBenchmark() => Value1 ^ Value2;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => Value1 != Value2;

        [Benchmark]
        public Vector<T> MultiplyOperatorBenchmark() => Value1 * Value2;

        [Benchmark]
        public Vector<T> OnesComplementOperatorBenchmark() => ~Value1;

        [Benchmark]
        public Vector<T> SubtractionOperatorBenchmark() => Value1 - Value2;

        [Benchmark]
        public Vector<T> UnaryNegateOperatorBenchmark() => -Value1;

        [Benchmark]
        public Vector<T> AbsBenchmark() => Vector.Abs(Value1);

        [Benchmark]
        public Vector<T> AddBenchmark() => Vector.Add(Value1, Value2);

        [Benchmark]
        public Vector<T> AndNotBenchmark() => Vector.AndNot(Value1, Value2);

        [Benchmark]
        public Vector<T> BitwiseAndBenchmark() => Vector.BitwiseAnd(Value1, Value2);

        [Benchmark]
        public Vector<T> BitwiseOrBenchmark() => Vector.BitwiseOr(Value1, Value2);

        [Benchmark]
        public Vector<T> ConditionalSelectBenchmark() => Vector.ConditionalSelect(Value1, Value2, Value3);

        [Benchmark]
        public Vector<T> DivideBenchmark() => Vector.Divide(Value1, Value2);

        [Benchmark]
        public T DotBenchmark() => Vector.Dot(Value1, Value2);

        [Benchmark]
        public Vector<T> EqualsStaticBenchmark() => Vector.Equals(Value1, Value2);

        [Benchmark]
        public bool EqualsAllBenchmark() => Vector.EqualsAll(Value1, Value2);

        [Benchmark]
        public bool EqualsAnyBenchmark() => Vector.EqualsAny(Value1, Value2);

        [Benchmark]
        public Vector<T> GreaterThanBenchmark() => Vector.GreaterThan(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAllBenchmark() => Vector.GreaterThanAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAnyBenchmark() => Vector.GreaterThanAny(Value1, Value2);

        [Benchmark]
        public Vector<T> GreaterThanOrEqualBenchmark() => Vector.GreaterThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAllBenchmark() => Vector.GreaterThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAnyBenchmark() => Vector.GreaterThanOrEqualAny(Value1, Value2);

        [Benchmark]
        public Vector<T> LessThanBenchmark() => Vector.LessThan(Value1, Value2);

        [Benchmark]
        public bool LessThanAllBenchmark() => Vector.LessThanAll(Value1, Value2);

        [Benchmark]
        public bool LessThanAnyBenchmark() => Vector.LessThanAny(Value1, Value2);

        [Benchmark]
        public Vector<T> LessThanOrEqualBenchmark() => Vector.LessThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAllBenchmark() => Vector.LessThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAnyBenchmark() => Vector.LessThanOrEqualAny(Value1, Value2);

        [Benchmark]
        public Vector<T> MaxBenchmark() => Vector.Max(Value1, Value2);

        [Benchmark]
        public Vector<T> MinBenchmark() => Vector.Min(Value1, Value2);

        [Benchmark]
        public Vector<T> MultiplyBenchmark() => Vector.Multiply(Value1, Value2);

        [Benchmark]
        public Vector<T> NegateBenchmark() => Vector.Negate(Value1);

        [Benchmark]
        public Vector<T> OnesComplementBenchmark() => Vector.OnesComplement(Value1);

        [Benchmark]
        public Vector<T> SquareRootBenchmark() => Vector.SquareRoot(Value1);

        [Benchmark]
        public Vector<T> SubtractBenchmark() => Vector.Subtract(Value1, Value2);

        [Benchmark]
        public Vector<T> XorBenchmark() => Vector.Xor(Value1, Value2);
    }
}