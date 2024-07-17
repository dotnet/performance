
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Loops
{
    [BenchmarkCategory(Categories.Runtime, Categories.JIT)]
    [GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
    public class StrengthReduction
    {
        private short[] _arrayShorts;
        private int[] _arrayInts;
        private long[] _arrayLongs;

        private S3[] _arrayS3;
        private S8[] _arrayS8;
        private S12[] _arrayS12;
        private S16[] _arrayS16;
        private S29[] _arrayS29;

        [GlobalSetup]
        public void Setup()
        {
            _arrayShorts = Enumerable.Range(0, 10000).Select(i => (short)i).ToArray();
            _arrayInts = Enumerable.Range(0, 10000).Select(i => i).ToArray();
            _arrayLongs = Enumerable.Range(0, 10000).Select(i => (long)i).ToArray();

            _arrayS3 = Enumerable.Range(0, 10000).Select(i => new S3 { A = (byte)i, B = (byte)i, C = (byte)i }).ToArray();
            _arrayS8 = Enumerable.Range(0, 10000).Select(i => new S8 { A = i, B = i, }).ToArray();
            _arrayS12 = Enumerable.Range(0, 10000).Select(i => new S12 { A = i, B = i, C = i, }).ToArray();
            _arrayS16 = Enumerable.Range(0, 10000).Select(i => new S16 { A = i, B = i, }).ToArray();
            _arrayS29 = Enumerable.Range(0, 10000).Select(i => new S29 { A = (byte)i, }).ToArray();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("short")]
        public int SumShortsArray()
        {
            return SumShortsWithArray(_arrayShorts);
        }

        [Benchmark, BenchmarkCategory("short")]
        public int SumShortsSpan()
        {
            return SumShortsWithSpan(_arrayShorts);
        }

        [Benchmark, BenchmarkCategory("short")]
        public int SumShortsArrayStrengthReduced()
        {
            return SumShortsStrengthReducedWithArray(_arrayShorts);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumShortsWithArray(short[] input)
        {
            int result = 0;
            // 'or' by 1 to make loop body slightly larger to work around
            // https://github.com/dotnet/runtime/issues/104665
            foreach (short s in input)
                result += s | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumShortsWithSpan(ReadOnlySpan<short> input)
        {
            int result = 0;
            foreach (short s in input)
                result += s | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumShortsStrengthReducedWithArray(short[] input)
        {
            int result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref short p = ref input[0];
                do
                {
                    result += p | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("int")]
        public int SumIntsArray()
        {
            return SumIntsWithArray(_arrayInts);
        }

        [Benchmark, BenchmarkCategory("int")]
        public int SumIntsSpan()
        {
            return SumIntsWithSpan(_arrayInts);
        }

        [Benchmark, BenchmarkCategory("int")]
        public int SumIntsArrayStrengthReduced()
        {
            return SumIntsStrengthReducedWithArray(_arrayInts);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumIntsWithArray(int[] input)
        {
            int result = 0;
            foreach (short s in input)
                result += s | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumIntsWithSpan(ReadOnlySpan<int> input)
        {
            int result = 0;
            foreach (int s in input)
                result += s | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumIntsStrengthReducedWithArray(int[] input)
        {
            int result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref int p = ref input[0];
                do
                {
                    result += p | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("long")]
        public long SumLongsArray()
        {
            return SumLongsWithArray(_arrayLongs);
        }

        [Benchmark, BenchmarkCategory("long")]
        public long SumLongsSpan()
        {
            return SumLongsWithSpan(_arrayLongs);
        }

        [Benchmark, BenchmarkCategory("long")]
        public long SumLongsArrayStrengthReduced()
        {
            return SumLongsStrengthReducedWithArray(_arrayLongs);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long SumLongsWithArray(long[] input)
        {
            long result = 0;
            foreach (long s in input)
                result += s | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long SumLongsWithSpan(ReadOnlySpan<long> input)
        {
            int result = 0;
            foreach (int s in input)
                result += s | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long SumLongsStrengthReducedWithArray(long[] input)
        {
            long result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref long p = ref input[0];
                do
                {
                    result += p | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("S3")]
        public int SumS3Array()
        {
            return SumS3WithArray(_arrayS3);
        }

        [Benchmark, BenchmarkCategory("S3")]
        public int SumS3Span()
        {
            return SumS3WithSpan(_arrayS3);
        }

        [Benchmark, BenchmarkCategory("S3")]
        public int SumS3ArrayStrengthReduced()
        {
            return SumS3StrengthReducedWithArray(_arrayS3);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS3WithArray(S3[] input)
        {
            int result = 0;
            foreach (S3 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS3WithSpan(ReadOnlySpan<S3> input)
        {
            int result = 0;
            foreach (S3 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS3StrengthReducedWithArray(S3[] input)
        {
            int result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref S3 p = ref input[0];
                do
                {
                    S3 s = p;
                    result += s.A | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("S8")]
        public int SumS8Array()
        {
            return SumS8WithArray(_arrayS8);
        }

        [Benchmark, BenchmarkCategory("S8")]
        public int SumS8Span()
        {
            return SumS8WithSpan(_arrayS8);
        }

        [Benchmark, BenchmarkCategory("S8")]
        public int SumS8ArrayStrengthReduced()
        {
            return SumS8StrengthReducedWithArray(_arrayS8);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS8WithArray(S8[] input)
        {
            int result = 0;
            foreach (S8 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS8WithSpan(ReadOnlySpan<S8> input)
        {
            int result = 0;
            foreach (S8 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS8StrengthReducedWithArray(S8[] input)
        {
            int result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref S8 p = ref input[0];
                do
                {
                    S8 s = p;
                    result += s.A | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("S12")]
        public int SumS12Array()
        {
            return SumS12WithArray(_arrayS12);
        }

        [Benchmark, BenchmarkCategory("S12")]
        public int SumS12Span()
        {
            return SumS12WithSpan(_arrayS12);
        }

        [Benchmark, BenchmarkCategory("S12")]
        public int SumS12ArrayStrengthReduced()
        {
            return SumS12StrengthReducedWithArray(_arrayS12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS12WithArray(S12[] input)
        {
            int result = 0;
            foreach (S12 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS12WithSpan(ReadOnlySpan<S12> input)
        {
            int result = 0;
            foreach (S12 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS12StrengthReducedWithArray(S12[] input)
        {
            int result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref S12 p = ref input[0];
                do
                {
                    S12 s = p;
                    result += s.A | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("S16")]
        public long SumS16Array()
        {
            return SumS16WithArray(_arrayS16);
        }

        [Benchmark, BenchmarkCategory("S16")]
        public long SumS16Span()
        {
            return SumS16WithSpan(_arrayS16);
        }

        [Benchmark, BenchmarkCategory("S16")]
        public long SumS16ArrayStrengthReduced()
        {
            return SumS16StrengthReducedWithArray(_arrayS16);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long SumS16WithArray(S16[] input)
        {
            long result = 0;
            foreach (S16 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long SumS16WithSpan(ReadOnlySpan<S16> input)
        {
            long result = 0;
            foreach (S16 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long SumS16StrengthReducedWithArray(S16[] input)
        {
            long result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref S16 p = ref input[0];
                do
                {
                    S16 s = p;
                    result += s.A | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("S29")]
        public int SumS29Array()
        {
            int sum = 0;
            //for (int i = 0; i < 100; i++)
                sum += SumS29WithArray(_arrayS29);
            return sum;
        }

        [Benchmark, BenchmarkCategory("S29")]
        public int SumS29Span()
        {
            int sum = 0;
            //for (int i = 0; i < 100; i++)
                sum += SumS29WithSpan(_arrayS29);
            return sum;
        }

        [Benchmark, BenchmarkCategory("S29")]
        public int SumS29ArrayStrengthReduced()
        {
            int sum = 0;
            //for (int i = 0; i < 100; i++)
                sum += SumS29StrengthReducedWithArray(_arrayS29);
            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS29WithArray(S29[] input)
        {
            int result = 0;
            foreach (S29 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS29WithSpan(ReadOnlySpan<S29> input)
        {
            int result = 0;
            foreach (S29 s in input)
                result += s.A | 1;
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int SumS29StrengthReducedWithArray(S29[] input)
        {
            int result = 0;
            uint length = (uint)input.Length;
            if (length > 0)
            {
                ref S29 p = ref input[0];
                do
                {
                    S29 s = p;
                    result += s.A | 1;
                    p = ref Unsafe.Add(ref p, 1);
                    length--;
                } while (length != 0);
            }

            return result;
        }

        private struct S3
        {
            public byte A, B, C;
        }

        private struct S5
        {
            public byte A, B, C, D, E;
        }

        private struct S6
        {
            public ushort A, B, C;
        }

        public struct S8
        {
            public int A, B;
        }

        public struct S12
        {
            public int A, B, C;
        }

        public struct S16
        {
            public long A, B;
        }

        [StructLayout(LayoutKind.Sequential, Size = 29)]
        public struct S29
        {
            public byte A;
        }
    }
}
