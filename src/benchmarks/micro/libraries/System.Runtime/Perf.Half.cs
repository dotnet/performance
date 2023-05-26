// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;

using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Half
    {
        private float[] bufferSourceSingle;
        private float[] bufferDestinationSingle;
        private Half[] bufferDestinationHalf;
        private Half[] bufferSourceHalf;

        public static IEnumerable<Half> Values => new Half[]
        {
            BitConverter.UInt16BitsToHalf(0x03ff),  //Maximum subnormal number in Half
            (Half)12345.0f /* same value used by other tests to compare the perf */,
            BitConverter.UInt16BitsToHalf(0x7dff)   //NaN
        };

        public static IEnumerable<float> SingleValues => new float[]
        {
            6.097555E-05f,
            12345.0f /* same value used by other tests to compare the perf */,
            65520.0f,   //Minimum value that is infinity in Half
            float.NaN
        };

        [GlobalSetup]
        public void Setup()
        {
            const int Samples = 65536;
            bufferDestinationSingle = new float[Samples];
            Half[] bSH = bufferSourceHalf = new Half[Samples];
            Span<Half> spanSH = bSH.AsSpan();
            for (var i = 0; i < spanSH.Length; i++)
            {
                spanSH[i] = BitConverter.UInt16BitsToHalf((ushort)i);
            }
            ref Half x9 = ref MemoryMarshal.GetReference(spanSH);
            int length = spanSH.Length;
            int olen = length - 2;
            var rng = new Random(12345);    //ValuesGenerator doesn't support exhaustive permutation
            for (var i = 0; i < olen; i++)
            {
                int x = rng.Next(i, length);
                (Unsafe.Add(ref x9, x), Unsafe.Add(ref x9, i)) = (Unsafe.Add(ref x9, i), Unsafe.Add(ref x9, x));
            }
            bufferDestinationHalf = new Half[Samples];
            bufferSourceSingle = bSH.Select(a => (float)a).ToArray();
        }
        #region Permuted

        [Benchmark]
        public void SingleToHalfPermutedSimple()
        {
            Span<float> bA = bufferSourceSingle.AsSpan();
            Span<Half> bD = bufferDestinationHalf.AsSpan();
            ref float rsi = ref MemoryMarshal.GetReference(bA);
            ref Half rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (Half)Unsafe.Add(ref rsi, i);
            }
        }

        [Benchmark]
        public void SingleToHalfPermutedUnrolled()
        {
            Span<float> bA = bufferSourceSingle.AsSpan();
            Span<Half> bD = bufferDestinationHalf.AsSpan();
            ref float rsi = ref MemoryMarshal.GetReference(bA);
            ref Half rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            var olen = length - 3;
            for (; i < olen; i += 4)
            {
                Unsafe.Add(ref rdi, i + 0) = (Half)Unsafe.Add(ref rsi, i + 0);
                Unsafe.Add(ref rdi, i + 1) = (Half)Unsafe.Add(ref rsi, i + 1);
                Unsafe.Add(ref rdi, i + 2) = (Half)Unsafe.Add(ref rsi, i + 2);
                Unsafe.Add(ref rdi, i + 3) = (Half)Unsafe.Add(ref rsi, i + 3);
            }
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (Half)Unsafe.Add(ref rsi, i);
            }
        }
        [Benchmark]
        public void HalfToSinglePermutedSimple()
        {
            Span<Half> bA = bufferSourceHalf.AsSpan();
            Span<float> bD = bufferDestinationSingle.AsSpan();
            ref Half rsi = ref MemoryMarshal.GetReference(bA);
            ref float rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (float)Unsafe.Add(ref rsi, i);
            }
        }

        [Benchmark]
        public void HalfToSinglePermutedUnrolled()
        {
            Span<Half> bA = bufferSourceHalf.AsSpan();
            Span<float> bD = bufferDestinationSingle.AsSpan();
            ref Half rsi = ref MemoryMarshal.GetReference(bA);
            ref float rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            var olen = length - 3;
            for (; i < olen; i += 4)
            {
                Unsafe.Add(ref rdi, i + 0) = (float)Unsafe.Add(ref rsi, i + 0);
                Unsafe.Add(ref rdi, i + 1) = (float)Unsafe.Add(ref rsi, i + 1);
                Unsafe.Add(ref rdi, i + 2) = (float)Unsafe.Add(ref rsi, i + 2);
                Unsafe.Add(ref rdi, i + 3) = (float)Unsafe.Add(ref rsi, i + 3);
            }
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (float)Unsafe.Add(ref rsi, i);
            }
        }

        #endregion

        #region Simple
        [Benchmark]
        [ArgumentsSource(nameof(SingleValues))]
        public Half SingleToHalfSimple(float value) => (Half)value;

        [Benchmark]
        [ArgumentsSource(nameof(Values))]
        public float HalfToSingleSimple(Half value) => (float)value;

        #endregion
    }
}
