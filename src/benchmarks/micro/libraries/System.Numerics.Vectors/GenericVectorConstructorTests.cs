// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public partial class Constructor
    {
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
        public Vector<Byte> SpanCastBenchmark_Byte() 
            => MemoryMarshal.Cast<byte, Vector<byte>>(new ReadOnlySpan<Byte>(_arrValues_Byte))[0];

        [Benchmark]
        public Vector<SByte> SpanCastBenchmark_SByte() 
            => MemoryMarshal.Cast<sbyte, Vector<sbyte>>(new ReadOnlySpan<SByte>(_arrValues_SByte))[0];

        [Benchmark]
        public Vector<UInt16> SpanCastBenchmark_UInt16() 
            => MemoryMarshal.Cast<ushort, Vector<ushort>>(new ReadOnlySpan<UInt16>(_arrValues_UInt16))[0];

        [Benchmark]
        public Vector<Int16> SpanCastBenchmark_Int16()
            => MemoryMarshal.Cast<short, Vector<short>>(new ReadOnlySpan<Int16>(_arrValues_Int16))[0];

        [Benchmark]
        public Vector<UInt32> SpanCastBenchmark_UInt32() 
            => MemoryMarshal.Cast<uint, Vector<uint>>(new ReadOnlySpan<UInt32>(_arrValues_UInt32))[0];

        [Benchmark]
        public Vector<Int32> SpanCastBenchmark_Int32() 
            => MemoryMarshal.Cast<int, Vector<int>>(new ReadOnlySpan<Int32>(_arrValues_Int32))[0];

        [Benchmark]
        public Vector<UInt64> SpanCastBenchmark_UInt64() 
            => MemoryMarshal.Cast<ulong, Vector<ulong>>(new ReadOnlySpan<UInt64>(_arrValues_UInt64))[0];

        [Benchmark]
        public Vector<Int64> SpanCastBenchmark_Int64() 
            => MemoryMarshal.Cast<long, Vector<long>>(new ReadOnlySpan<Int64>(_arrValues_Int64))[0];

        [Benchmark]
        public Vector<Single> SpanCastBenchmark_Single() 
            => MemoryMarshal.Cast<float, Vector<float>>(new ReadOnlySpan<Single>(_arrValues_Single))[0];

        [Benchmark]
        public Vector<Double> SpanCastBenchmark_Double() 
            => MemoryMarshal.Cast<double, Vector<double>>(new ReadOnlySpan<Double>(_arrValues_Double))[0];

        private static T[] GenerateRandomValuesForVector<T>(int minValue, int maxValue) where T : struct 
            => Util.GenerateRandomValues<T>(Vector<T>.Count, minValue, maxValue);
    }
}