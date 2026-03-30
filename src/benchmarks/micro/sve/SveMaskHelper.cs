// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;

namespace SveBenchmarks
{
    // Compatibility shim for CreateWhileLessThanMask API rename in .NET 11.
    // In .NET 9/10 the methods are named CreateWhileLessThanMask{8,16,32,64}Bit.
    // In .NET 11+ they were renamed to CreateWhileLessThanMask{Byte,Int16,UInt16,...}.
    internal static class SveMaskHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<byte> CreateWhileLessThanMaskByte(int left, int right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskByte(left, right);
#else
            return Sve.CreateWhileLessThanMask8Bit(left, right);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<ushort> CreateWhileLessThanMaskUInt16(int left, int right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskUInt16(left, right);
#else
            return Sve.CreateWhileLessThanMask16Bit(left, right);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<short> CreateWhileLessThanMaskInt16(int left, int right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskInt16(left, right);
#else
            return (Vector<short>)Sve.CreateWhileLessThanMask16Bit(left, right);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<uint> CreateWhileLessThanMaskUInt32(int left, int right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskUInt32(left, right);
#else
            return Sve.CreateWhileLessThanMask32Bit(left, right);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<int> CreateWhileLessThanMaskInt32(int left, int right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskInt32(left, right);
#else
            return (Vector<int>)Sve.CreateWhileLessThanMask32Bit(left, right);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<float> CreateWhileLessThanMaskSingle(int left, int right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskSingle(left, right);
#else
            return (Vector<float>)Sve.CreateWhileLessThanMask32Bit(left, right);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<ulong> CreateWhileLessThanMaskUInt64(int left, int right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskUInt64(left, right);
#else
            return Sve.CreateWhileLessThanMask64Bit(left, right);
#endif
        }

        // long overloads for benchmarks that use long loop counters (e.g. Partition.cs)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector<uint> CreateWhileLessThanMaskUInt32(long left, long right)
        {
#if NET11_0_OR_GREATER
            return Sve.CreateWhileLessThanMaskUInt32(left, right);
#else
            return Sve.CreateWhileLessThanMask32Bit(left, right);
#endif
        }
    }
}
