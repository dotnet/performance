// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.SIMD)]
    public partial class Constructor
    {
        public const int DefaultInnerIterationsCount = 100000000;

        Byte[] _arrValues_Byte = GenerateRandomValuesForVector<Byte>(Byte.MinValue, Byte.MaxValue);
        SByte[] _arrValues_SByte = GenerateRandomValuesForVector<SByte>(SByte.MinValue, SByte.MaxValue);
        UInt16[] _arrValues_UInt16 = GenerateRandomValuesForVector<UInt16>(UInt16.MinValue, UInt16.MaxValue);
        Int16[] _arrValues_Int16 = GenerateRandomValuesForVector<Int16>(Int16.MinValue, Int16.MaxValue);
        UInt32[] _arrValues_UInt32 = GenerateRandomValuesForVector<UInt32>(Int32.MinValue, Int32.MaxValue);
        Int32[] _arrValues_Int32 = GenerateRandomValuesForVector<Int32>(Int32.MinValue, Int32.MaxValue);
        UInt64[] _arrValues_UInt64 = GenerateRandomValuesForVector<UInt64>(Int32.MinValue, Int32.MaxValue);
        Int64[] _arrValues_Int64 = GenerateRandomValuesForVector<Int64>(Int32.MinValue, Int32.MaxValue);
        Single[] _arrValues_Single = GenerateRandomValuesForVector<Single>(Int32.MinValue, Int32.MaxValue);
        Double[] _arrValues_Double = GenerateRandomValuesForVector<Double>(Int32.MinValue, Int32.MaxValue);

        [Benchmark]
        public void SpanCastBenchmark_Byte() => SpanCast(new ReadOnlySpan<Byte>(_arrValues_Byte));

        [Benchmark]
        public void SpanCastBenchmark_SByte() => SpanCast(new ReadOnlySpan<SByte>(_arrValues_SByte));

        [Benchmark]
        public void SpanCastBenchmark_UInt16() => SpanCast(new ReadOnlySpan<UInt16>(_arrValues_UInt16));

        [Benchmark]
        public void SpanCastBenchmark_Int16() => SpanCast(new ReadOnlySpan<Int16>(_arrValues_Int16));

        [Benchmark]
        public void SpanCastBenchmark_UInt32() => SpanCast(new ReadOnlySpan<UInt32>(_arrValues_UInt32));

        [Benchmark]
        public void SpanCastBenchmark_Int32() => SpanCast(new ReadOnlySpan<Int32>(_arrValues_Int32));

        [Benchmark]
        public void SpanCastBenchmark_UInt64() => SpanCast(new ReadOnlySpan<UInt64>(_arrValues_UInt64));

        [Benchmark]
        public void SpanCastBenchmark_Int64() => SpanCast(new ReadOnlySpan<Int64>(_arrValues_Int64));

        [Benchmark]
        public void SpanCastBenchmark_Single() => SpanCast(new ReadOnlySpan<Single>(_arrValues_Single));

        [Benchmark]
        public void SpanCastBenchmark_Double() => SpanCast(new ReadOnlySpan<Double>(_arrValues_Double));

        public static void SpanCast<T>(ReadOnlySpan<T> values) where T : struct
        {
            for (var iteration = 0; iteration < DefaultInnerIterationsCount; iteration++)
            {
                ReadOnlySpan<Vector<T>> vectors = MemoryMarshal.Cast<T, Vector<T>>(values);
                Vector<T> vector = vectors[0];
            }
        }

        private static T[] GenerateRandomValuesForVector<T>(int minValue, int maxValue) where T : struct 
            => Util.GenerateRandomValues<T>(Vector<T>.Count, minValue, maxValue);
    }
}