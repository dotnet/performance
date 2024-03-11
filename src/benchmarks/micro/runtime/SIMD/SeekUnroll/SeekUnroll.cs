// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

[BenchmarkCategory(Categories.Runtime, Categories.SIMD, Categories.JIT)]
public class SeekUnroll
{
    // The purpose of this micro-benchmark is to measure the effect of unrolling
    // on this loop (taken from https://github.com/aspnet/KestrelHttpServer/pull/1138)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int FindByte(ref Vector<byte> byteEquals)
    {
        var vector64 = Vector.AsVectorInt64(byteEquals);
        long longValue = 0;
        var i = 0;
        for (; i < Vector<long>.Count; i++)
        {
            longValue = vector64[i];
            if (longValue == 0) continue;
            break;
        }

        // Flag least significant power of two bit
        var powerOfTwoFlag = (ulong)(longValue ^ (longValue - 1));
        // Shift all powers of two into the high byte and extract
        var foundByteIndex = (int)((powerOfTwoFlag * _xorPowerOfTwoToHighByte) >> 57);
        // Single LEA instruction with jitted const (using function result)
        return i * 8 + foundByteIndex;
    }

    // Magic constant used in FindByte
    const ulong _xorPowerOfTwoToHighByte = (0x07ul |
                                            0x06ul << 8 |
                                            0x05ul << 16 |
                                            0x04ul << 24 |
                                            0x03ul << 32 |
                                            0x02ul << 40 |
                                            0x01ul << 48) + 1;

    // Inner loop to repeatedly call FindByte
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void InnerLoop(ref int foundIndex, ref Vector<Byte> vector)
    {
        for (int i = 0; i < InnerIterations; i++)
        {
            foundIndex = FindByte(ref vector);
        }
    }

    static int InnerIterations = 1000000000;

    [Benchmark(Description = nameof(SeekUnroll))]
    [ArgumentsSource(nameof(ArrayedBoxedIndicesToTest))]
    [BenchmarkCategory(Categories.NoInterpreter)]
    [MemoryRandomization]
    public bool Test(int boxedIndex) 
    {
        int index = boxedIndex;
        if (index >= Vector<Byte>.Count)
        {
            // FindByte assumes index is in range
            index = 0;
        }
        var bytes = new Byte[Vector<Byte>.Count];
        bytes[index] = 255;
        Vector<Byte> vector = new Vector<Byte>(bytes);

        int foundIndex = -1;

        InnerLoop(ref foundIndex, ref vector);

        return (index == foundIndex);
    }

    public static IEnumerable<object> ArrayedBoxedIndicesToTest => new object[] { 1, 3, 11, 19, 27 };
}
