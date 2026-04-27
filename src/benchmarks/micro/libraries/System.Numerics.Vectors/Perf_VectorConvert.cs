// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Numerics.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.SIMD, Categories.JIT)]
    public class Perf_VectorConvert
    {
        private const int Iterations = 1000;

        // These arrays are used for the Narrow benchmarks, so they need 2 vectors per iteration
        private static readonly double[] s_valuesDouble = ValuesGenerator.Array<double>(Vector<double>.Count * 2 * Iterations);
        private static readonly short[] s_valuesShort = ValuesGenerator.Array<short>(Vector<short>.Count * 2 * Iterations);
        private static readonly ushort[] s_valuesUShort = ValuesGenerator.Array<ushort>(Vector<ushort>.Count * 2 * Iterations);
        private static readonly int[] s_valuesInt = ValuesGenerator.Array<int>(Vector<int>.Count * 2 * Iterations);
        private static readonly uint[] s_valuesUInt = ValuesGenerator.Array<uint>(Vector<uint>.Count * 2 * Iterations);
        private static readonly long[] s_valuesLong = ValuesGenerator.Array<long>(Vector<long>.Count * 2 * Iterations);
        private static readonly ulong[] s_valuesULong = ValuesGenerator.Array<ulong>(Vector<ulong>.Count * 2 * Iterations);

        // The remainder are used only for Widen and same-size Convert benchmarks
        private static readonly float[] s_valuesFloat = ValuesGenerator.Array<float>(Vector<float>.Count * Iterations);
        private static readonly byte[] s_valuesByte = ValuesGenerator.Array<byte>(Vector<byte>.Count * Iterations);
        private static readonly sbyte[] s_valuesSByte = ValuesGenerator.Array<sbyte>(Vector<sbyte>.Count * Iterations);

        // Vector.ConvertToXXX
        [Benchmark]
        public Vector<int> Convert_float_int() => Convert<float, int>(s_valuesFloat);

        [Benchmark]
        [MemoryRandomization]
        public Vector<uint> Convert_float_uint() => Convert<float, uint>(s_valuesFloat);

        [Benchmark]
        [MemoryRandomization]
        public Vector<long> Convert_double_long() => Convert<double, long>(s_valuesDouble);

        [Benchmark]
        public Vector<ulong> Convert_double_ulong() => Convert<double, ulong>(s_valuesDouble);

        [Benchmark]
        public Vector<float> Convert_int_float() => Convert<int, float>(s_valuesInt);

        [Benchmark]
        public Vector<float> Convert_uint_float() => Convert<uint, float>(s_valuesUInt);

        [Benchmark]
        public Vector<double> Convert_long_double() => Convert<long, double>(s_valuesLong);

        [Benchmark]
        public Vector<double> Convert_ulong_double() => Convert<ulong, double>(s_valuesULong);

        // Vector.Narrow
        [Benchmark]
        public Vector<float> Narrow_double() => Narrow<double, float>(s_valuesDouble);

        [Benchmark]
        public Vector<sbyte> Narrow_short() => Narrow<short, sbyte>(s_valuesShort);

        [Benchmark]
        public Vector<byte> Narrow_ushort() => Narrow<ushort, byte>(s_valuesUShort);

        [Benchmark]
        public Vector<short> Narrow_int() => Narrow<int, short>(s_valuesInt);

        [Benchmark]
        public Vector<ushort> Narrow_uint() => Narrow<uint, ushort>(s_valuesUInt);

        [Benchmark]
        public Vector<int> Narrow_long() => Narrow<long, int>(s_valuesLong);

        [Benchmark]
        public Vector<uint> Narrow_ulong() => Narrow<ulong, uint>(s_valuesULong);

        // Vector.Widen
        [Benchmark]
        public Vector<double> Widen_float() => Widen<float, double>(s_valuesFloat);

        [Benchmark]
        public Vector<short> Widen_sbyte() => Widen<sbyte, short>(s_valuesSByte);

        [Benchmark]
        public Vector<ushort> Widen_byte() => Widen<byte, ushort>(s_valuesByte);

        [Benchmark]
        public Vector<int> Widen_short() => Widen<short, int>(s_valuesShort);

        [Benchmark]
        public Vector<uint> Widen_ushort() => Widen<ushort, uint>(s_valuesUShort);

        [Benchmark]
        public Vector<long> Widen_int() => Widen<int, long>(s_valuesInt);

        [Benchmark]
        public Vector<ulong> Widen_uint() => Widen<uint, ulong>(s_valuesUInt);

        private static Vector<TTo> Convert<TFrom, TTo>(TFrom[] values) where TFrom : struct where TTo : struct
        {
            var input = Unsafe.As<Vector<TFrom>[]>(values);
            var accum = Vector<TTo>.Zero;

            ref Vector<TFrom> ptr = ref input[0];
            for (int i = Iterations; i >= 0; --i)
            {
                accum ^= ConvertVector<TFrom, TTo>(ptr);
                ptr = ref Unsafe.Add(ref ptr, 1);
            }

            return accum;
        }

        private static Vector<TTo> Narrow<TFrom, TTo>(TFrom[] values) where TFrom : struct where TTo : struct
        {
            var input = Unsafe.As<Vector<TFrom>[]>(values);
            var accum = Vector<TTo>.Zero;

            ref Vector<TFrom> ptr = ref input[0];
            for (int i = Iterations; i >= 0; --i)
            {
                accum ^= NarrowVector<TFrom, TTo>(ptr, Unsafe.Add(ref ptr, 1));
                ptr = ref Unsafe.Add(ref ptr, 2);
            }

            return accum;
        }

        private static Vector<TTo> Widen<TFrom, TTo>(TFrom[] values) where TFrom : struct where TTo : struct
        {
            var input = Unsafe.As<Vector<TFrom>[]>(values);
            var accum = Vector<TTo>.Zero;

            ref Vector<TFrom> ptr = ref input[0];
            for (int i = Iterations; i >= 0; --i)
            {
                accum ^= WidenVector<TFrom, TTo>(ptr);
                ptr = ref Unsafe.Add(ref ptr, 1);
            }

            return accum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<TTo> ConvertVector<TFrom, TTo>(Vector<TFrom> value) where TFrom : struct where TTo : struct
        {
            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(int))
                return (Vector<TTo>)(object)Vector.ConvertToInt32((Vector<float>)(object)value);
            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(uint))
                return (Vector<TTo>)(object)Vector.ConvertToUInt32((Vector<float>)(object)value);
            if (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(long))
                return (Vector<TTo>)(object)Vector.ConvertToInt64((Vector<double>)(object)value);
            if (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(ulong))
                return (Vector<TTo>)(object)Vector.ConvertToUInt64((Vector<double>)(object)value);
            if (typeof(TFrom) == typeof(int) && typeof(TTo) == typeof(float))
                return (Vector<TTo>)(object)Vector.ConvertToSingle((Vector<int>)(object)value);
            if (typeof(TFrom) == typeof(uint) && typeof(TTo) == typeof(float))
                return (Vector<TTo>)(object)Vector.ConvertToSingle((Vector<uint>)(object)value);
            if (typeof(TFrom) == typeof(long) && typeof(TTo) == typeof(double))
                return (Vector<TTo>)(object)Vector.ConvertToDouble((Vector<long>)(object)value);
            if (typeof(TFrom) == typeof(ulong) && typeof(TTo) == typeof(double))
                return (Vector<TTo>)(object)Vector.ConvertToDouble((Vector<ulong>)(object)value);

            throw new NotSupportedException("Type combination unsupported for Vector.ConvertToXXX");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<TTo> NarrowVector<TFrom, TTo>(Vector<TFrom> value1, Vector<TFrom> value2) where TFrom : struct where TTo : struct
        {
            if (typeof(TFrom) == typeof(double) && typeof(TTo) == typeof(float))
                return (Vector<TTo>)(object)Vector.Narrow((Vector<double>)(object)value1, (Vector<double>)(object)value2);
            if (typeof(TFrom) == typeof(short) && typeof(TTo) == typeof(sbyte))
                return (Vector<TTo>)(object)Vector.Narrow((Vector<short>)(object)value1, (Vector<short>)(object)value2);
            if (typeof(TFrom) == typeof(ushort) && typeof(TTo) == typeof(byte))
                return (Vector<TTo>)(object)Vector.Narrow((Vector<ushort>)(object)value1, (Vector<ushort>)(object)value2);
            if (typeof(TFrom) == typeof(int) && typeof(TTo) == typeof(short))
                return (Vector<TTo>)(object)Vector.Narrow((Vector<int>)(object)value1, (Vector<int>)(object)value2);
            if (typeof(TFrom) == typeof(uint) && typeof(TTo) == typeof(ushort))
                return (Vector<TTo>)(object)Vector.Narrow((Vector<uint>)(object)value1, (Vector<uint>)(object)value2);
            if (typeof(TFrom) == typeof(long) && typeof(TTo) == typeof(int))
                return (Vector<TTo>)(object)Vector.Narrow((Vector<long>)(object)value1, (Vector<long>)(object)value2);
            if (typeof(TFrom) == typeof(ulong) && typeof(TTo) == typeof(uint))
                return (Vector<TTo>)(object)Vector.Narrow((Vector<ulong>)(object)value1, (Vector<ulong>)(object)value2);

            throw new NotSupportedException("Type combination unsupported for Vector.Narrow");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector<TTo> WidenVector<TFrom, TTo>(Vector<TFrom> value) where TFrom : struct where TTo : struct
        {
            if (typeof(TFrom) == typeof(float) && typeof(TTo) == typeof(double))
            {
                Vector.Widen((Vector<float>)(object)value, out var out1, out var out2);
                return (Vector<TTo>)(object)(out1 ^ out2);
            }
            if (typeof(TFrom) == typeof(sbyte) && typeof(TTo) == typeof(short))
            {
                Vector.Widen((Vector<sbyte>)(object)value, out var out1, out var out2);
                return (Vector<TTo>)(object)(out1 ^ out2);
            }
            if (typeof(TFrom) == typeof(byte) && typeof(TTo) == typeof(ushort))
            {
                Vector.Widen((Vector<byte>)(object)value, out var out1, out var out2);
                return (Vector<TTo>)(object)(out1 ^ out2);
            }
            if (typeof(TFrom) == typeof(short) && typeof(TTo) == typeof(int))
            {
                Vector.Widen((Vector<short>)(object)value, out var out1, out var out2);
                return (Vector<TTo>)(object)(out1 ^ out2);
            }
            if (typeof(TFrom) == typeof(ushort) && typeof(TTo) == typeof(uint))
            {
                Vector.Widen((Vector<ushort>)(object)value, out var out1, out var out2);
                return (Vector<TTo>)(object)(out1 ^ out2);
            }
            if (typeof(TFrom) == typeof(int) && typeof(TTo) == typeof(long))
            {
                Vector.Widen((Vector<int>)(object)value, out var out1, out var out2);
                return (Vector<TTo>)(object)(out1 ^ out2);
            }
            if (typeof(TFrom) == typeof(uint) && typeof(TTo) == typeof(ulong))
            {
                Vector.Widen((Vector<uint>)(object)value, out var out1, out var out2);
                return (Vector<TTo>)(object)(out1 ^ out2);
            }

            throw new NotSupportedException("Type combination unsupported for Vector.Widen");
        }
    }
}