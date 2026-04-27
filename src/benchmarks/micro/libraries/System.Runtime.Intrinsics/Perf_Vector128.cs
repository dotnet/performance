// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Runtime.Intrinsics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Vector128
    {
        private static readonly Vector128<double> _vectorDouble = Vector128.Create(1.0, 2.0);
        private static readonly Vector128<float> _vectorFloat = Vector128.Create(1.0F, 2.0F, 3.0F, 4.0F);
        private static readonly Vector128<long> _vectorLong = Vector128.Create(1, 2);
        private static readonly Vector128<ulong> _vectorULong = Vector128.Create((ulong)1, 2);
        private static readonly Vector128<int> _vectorInt = Vector128.Create(1, 2, 3, 4);
        private static readonly Vector128<uint> _vectorUInt = Vector128.Create((uint)1, 2, 3, 4);
        
        [Benchmark]
        public Vector128<double> FloorDoubleBenchmark() => Vector128.Floor(_vectorDouble);

        [Benchmark]
        public Vector128<float> FloorFloatBenchmark() => Vector128.Floor(_vectorFloat);

        [Benchmark]
        public Vector128<double> CeilingDoubleBenchmark() => Vector128.Ceiling(_vectorDouble);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<float> CeilingFloatBenchmark() => Vector128.Ceiling(_vectorFloat);

        [Benchmark]
        public Vector128<double> ConvertLongToDoubleBenchmark() => Vector128.ConvertToDouble(_vectorLong);

        [Benchmark]
        public Vector128<double> ConvertULongToDoubleBenchmark() => Vector128.ConvertToDouble(_vectorULong);

        [Benchmark]
        public Vector128<int> ConvertFloatToIntBenchmark() => Vector128.ConvertToInt32(_vectorFloat);

        [Benchmark]
        public Vector128<long> ConvertDoubleToLongBenchmark() => Vector128.ConvertToInt64(_vectorDouble);

        [Benchmark]
        public Vector128<float> ConvertIntToFloatBenchmark() => Vector128.ConvertToSingle(_vectorInt);

        [Benchmark]
        public Vector128<float> ConvertUIntToFloatBenchmark() => Vector128.ConvertToSingle(_vectorUInt);

        [Benchmark]
        public Vector128<uint> ConvertFloatToUIntBenchmark() => Vector128.ConvertToUInt32(_vectorFloat);
        
        [Benchmark]
        public Vector128<ulong> ConvertDoubleToULongBenchmark() => Vector128.ConvertToUInt64(_vectorDouble);
    }

    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Vector128Float
    {
        private static readonly Vector128<float> Value1 = Vector128<float>.AllBitsSet;
        private static readonly Vector128<float> Value2 = Vector128<float>.AllBitsSet + Vector128<float>.AllBitsSet;
        private static readonly Vector128<float> Value3 = Vector128<float>.AllBitsSet + Vector128<float>.AllBitsSet + Vector128<float>.AllBitsSet;

        [Benchmark]
        public int CountBenchmark() => Vector128<float>.Count;

        [Benchmark]
        public Vector128<float> AllBitsSetBenchmark() => Vector128<float>.AllBitsSet;

        [Benchmark]
        public Vector128<float> ZeroBenchmark() => Vector128<float>.Zero;

        [Benchmark]
        public bool EqualsBenchmark() => Value1.Equals(Value2);

        [Benchmark]
        public int GetHashCodeBenchmark() => Value1.GetHashCode();

        [Benchmark]
        public Vector128<float> AddOperatorBenchmark() => Value1 + Value2;

        [Benchmark]
        public Vector128<float> BitwiseAndOperatorBenchmark() => Value1 & Value2;

        [Benchmark]
        public Vector128<float> BitwiseOrOperatorBenchmark() => Value1 | Value2;

        [Benchmark]
        public Vector128<float> DivisionOperatorBenchmark() => Value1 / Value2;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => Value1 == Value2;

        [Benchmark]
        public Vector128<float> ExclusiveOrOperatorBenchmark() => Value1 ^ Value2;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => Value1 != Value2;

        [Benchmark]
        public Vector128<float> MultiplyOperatorBenchmark() => Value1 * Value2;

        [Benchmark]
        public Vector128<float> OnesComplementOperatorBenchmark() => ~Value1;

        [Benchmark]
        public Vector128<float> SubtractionOperatorBenchmark() => Value1 - Value2;

        [Benchmark]
        public Vector128<float> UnaryNegateOperatorBenchmark() => -Value1;

        [Benchmark]
        public Vector128<float> AbsBenchmark() => Vector128.Abs(Value1);

        [Benchmark]
        public Vector128<float> AddBenchmark() => Vector128.Add(Value1, Value2);

        [Benchmark]
        public Vector128<float> AndNotBenchmark() => Vector128.AndNot(Value1, Value2);

        [Benchmark]
        public Vector128<float> BitwiseAndBenchmark() => Vector128.BitwiseAnd(Value1, Value2);

        [Benchmark]
        public Vector128<float> BitwiseOrBenchmark() => Vector128.BitwiseOr(Value1, Value2);

        [Benchmark]
        public Vector128<float> ConditionalSelectBenchmark() => Vector128.ConditionalSelect(Value1, Value2, Value3);

        [Benchmark]
        public Vector128<float> DivideBenchmark() => Vector128.Divide(Value1, Value2);

        [Benchmark]
        public float DotBenchmark() => Vector128.Dot(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<float> EqualsStaticBenchmark() => Vector128.Equals(Value1, Value2);

        [Benchmark]
        public bool EqualsAllBenchmark() => Vector128.EqualsAll(Value1, Value2);

        [Benchmark]
        public bool EqualsAnyBenchmark() => Vector128.EqualsAny(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<float> GreaterThanBenchmark() => Vector128.GreaterThan(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAllBenchmark() => Vector128.GreaterThanAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAnyBenchmark() => Vector128.GreaterThanAny(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<float> GreaterThanOrEqualBenchmark() => Vector128.GreaterThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAllBenchmark() => Vector128.GreaterThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAnyBenchmark() => Vector128.GreaterThanOrEqualAny(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<float> LessThanBenchmark() => Vector128.LessThan(Value1, Value2);

        [Benchmark]
        public bool LessThanAllBenchmark() => Vector128.LessThanAll(Value1, Value2);

        [Benchmark]
        public bool LessThanAnyBenchmark() => Vector128.LessThanAny(Value1, Value2);

        [Benchmark]
        public Vector128<float> LessThanOrEqualBenchmark() => Vector128.LessThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAllBenchmark() => Vector128.LessThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAnyBenchmark() => Vector128.LessThanOrEqualAny(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<float> MaxBenchmark() => Vector128.Max(Value1, Value2);

        [Benchmark]
        public Vector128<float> MinBenchmark() => Vector128.Min(Value1, Value2);

        [Benchmark]
        public Vector128<float> MultiplyBenchmark() => Vector128.Multiply(Value1, Value2);

        [Benchmark]
        public Vector128<float> NegateBenchmark() => Vector128.Negate(Value1);

        [Benchmark]
        public Vector128<float> OnesComplementBenchmark() => Vector128.OnesComplement(Value1);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<float> SquareRootBenchmark() => Vector128.Sqrt(Value1);

        [Benchmark]
        public Vector128<float> SubtractBenchmark() => Vector128.Subtract(Value1, Value2);

        [Benchmark]
        public float SumBenchmark() =>  Vector128.Sum(Value1);

        [Benchmark]
        public Vector128<float> XorBenchmark() => Vector128.Xor(Value1, Value2);
    }

    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_Vector128Int
    {
        private static readonly Vector128<int> Value1 = Vector128<int>.AllBitsSet;
        private static readonly Vector128<int> Value2 = Vector128<int>.AllBitsSet + Vector128<int>.AllBitsSet;
        private static readonly Vector128<int> Value3 = Vector128<int>.AllBitsSet + Vector128<int>.AllBitsSet + Vector128<int>.AllBitsSet;

        [Benchmark]
        public int CountBenchmark() => Vector128<int>.Count;

        [Benchmark]
        public Vector128<int> AllBitsSetBenchmark() => Vector128<int>.AllBitsSet;

        [Benchmark]
        public Vector128<int> ZeroBenchmark() => Vector128<int>.Zero;

        [Benchmark]
        public bool EqualsBenchmark() => Value1.Equals(Value2);

        [Benchmark]
        public int GetHashCodeBenchmark() => Value1.GetHashCode();

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> AddOperatorBenchmark() => Value1 + Value2;

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> BitwiseAndOperatorBenchmark() => Value1 & Value2;

        [Benchmark]
        public Vector128<int> BitwiseOrOperatorBenchmark() => Value1 | Value2;

        [Benchmark]
        public Vector128<int> DivisionOperatorBenchmark() => Value1 / Value2;

        [Benchmark]
        public bool EqualityOperatorBenchmark() => Value1 == Value2;

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> ExclusiveOrOperatorBenchmark() => Value1 ^ Value2;

        [Benchmark]
        public bool InequalityOperatorBenchmark() => Value1 != Value2;

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> MultiplyOperatorBenchmark() => Value1 * Value2;

        [Benchmark]
        public Vector128<int> OnesComplementOperatorBenchmark() => ~Value1;

        [Benchmark]
        public Vector128<int> SubtractionOperatorBenchmark() => Value1 - Value2;

        [Benchmark]
        public Vector128<int> UnaryNegateOperatorBenchmark() => -Value1;

        [Benchmark]
        public Vector128<int> AbsBenchmark() => Vector128.Abs(Value1);

        [Benchmark]
        public Vector128<int> AddBenchmark() => Vector128.Add(Value1, Value2);

        [Benchmark]
        public Vector128<int> AndNotBenchmark() => Vector128.AndNot(Value1, Value2);

        [Benchmark]
        public Vector128<int> BitwiseAndBenchmark() => Vector128.BitwiseAnd(Value1, Value2);

        [Benchmark]
        public Vector128<int> BitwiseOrBenchmark() => Vector128.BitwiseOr(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> ConditionalSelectBenchmark() => Vector128.ConditionalSelect(Value1, Value2, Value3);

        [Benchmark]
        public Vector128<int> DivideBenchmark() => Vector128.Divide(Value1, Value2);

        [Benchmark]
        public int DotBenchmark() => Vector128.Dot(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> EqualsStaticBenchmark() => Vector128.Equals(Value1, Value2);

        [Benchmark]
        public bool EqualsAllBenchmark() => Vector128.EqualsAll(Value1, Value2);

        [Benchmark]
        public bool EqualsAnyBenchmark() => Vector128.EqualsAny(Value1, Value2);

        [Benchmark]
        public Vector128<int> GreaterThanBenchmark() => Vector128.GreaterThan(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAllBenchmark() => Vector128.GreaterThanAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanAnyBenchmark() => Vector128.GreaterThanAny(Value1, Value2);

        [Benchmark]
        public Vector128<int> GreaterThanOrEqualBenchmark() => Vector128.GreaterThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAllBenchmark() => Vector128.GreaterThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool GreaterThanOrEqualAnyBenchmark() => Vector128.GreaterThanOrEqualAny(Value1, Value2);

        [Benchmark]
        public Vector128<int> LessThanBenchmark() => Vector128.LessThan(Value1, Value2);

        [Benchmark]
        public bool LessThanAllBenchmark() => Vector128.LessThanAll(Value1, Value2);

        [Benchmark]
        public bool LessThanAnyBenchmark() => Vector128.LessThanAny(Value1, Value2);

        [Benchmark]
        public Vector128<int> LessThanOrEqualBenchmark() => Vector128.LessThanOrEqual(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAllBenchmark() => Vector128.LessThanOrEqualAll(Value1, Value2);

        [Benchmark]
        public bool LessThanOrEqualAnyBenchmark() => Vector128.LessThanOrEqualAny(Value1, Value2);

        [Benchmark]
        public Vector128<int> MaxBenchmark() => Vector128.Max(Value1, Value2);

        [Benchmark]
        public Vector128<int> MinBenchmark() => Vector128.Min(Value1, Value2);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> MultiplyBenchmark() => Vector128.Multiply(Value1, Value2);

        [Benchmark]
        public Vector128<int> NegateBenchmark() => Vector128.Negate(Value1);

        [Benchmark]
        public Vector128<int> OnesComplementBenchmark() => Vector128.OnesComplement(Value1);

        [Benchmark]
        [MemoryRandomization]
        public Vector128<int> SquareRootBenchmark() => Vector128.Sqrt(Value1);

        [Benchmark]
        public Vector128<int> SubtractBenchmark() => Vector128.Subtract(Value1, Value2);

        [Benchmark]
        public int SumBenchmark() =>  Vector128.Sum(Value1);

        [Benchmark]
        public Vector128<int> XorBenchmark() => Vector128.Xor(Value1, Value2);
    }
}