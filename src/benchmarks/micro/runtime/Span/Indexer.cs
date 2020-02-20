// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Span
{
    public class Sink
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Sink NewSink() { return new Sink(); }

        public byte b = 0;
        public int i;
    }

    [BenchmarkCategory(Categories.Runtime, Categories.Span)]
    public class IndexerBench
    {
        [Params(1024)]
        public int length; 

        private byte[] a;
        private Sink sink = Sink.NewSink();
        
        [GlobalSetup]
        public void Setup() => a = GetData(length);

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Ref() => TestRef(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestRef(Span<byte> data)
        {
            ref byte p = ref MemoryMarshal.GetReference(data);
            int length = data.Length;
            byte x = 0;

            for (var idx = 0; idx < length; idx++)
            {
                x ^= Unsafe.Add(ref p, idx);
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Fixed1() => TestFixed1(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe byte TestFixed1(Span<byte> data)
        {
            fixed (byte* pData = &MemoryMarshal.GetReference(data))
            {
                int length = data.Length;
                byte x = 0;
                byte* p = pData;

                for (var idx = 0; idx < length; idx++)
                {
                    x ^= *(p + idx);
                }

                return x;
            }
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Fixed2() => TestFixed2(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe byte TestFixed2(Span<byte> data)
        {
            fixed (byte* pData = &MemoryMarshal.GetReference(data))
            {
                int length = data.Length;
                byte x = 0;

                for (var idx = 0; idx < length; idx++)
                {
                    x ^= pData[idx];
                }

                return x;
            }
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Indexer1() => TestIndexer1(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer1(Span<byte> data)
        {
            int length = data.Length;
            byte x = 0;

            for (var idx = 0; idx < length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Indexer2() => TestIndexer2(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer2(Span<byte> data)
        {
            byte x = 0;

            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Indexer3() => TestIndexer3(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer3(Span<byte> data)
        {
            Span<byte> data2 = data;

            byte x = 0;

            for (var idx = 0; idx < data2.Length; idx++)
            {
                x ^= data2[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Indexer4() => TestIndexer4(a, 10);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer4(Span<byte> data, int iterations)
        {
            byte x = 0;
            int length = data.Length;

            // This does more or less the same work as TestIndexer1
            // but is expressed as a loop nest.
            for (int i = 0; i < iterations; i++)
            {
                x = 0;

                for (var idx = 0; idx < length; idx++)
                {
                    x ^= data[idx];
                }
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Indexer5() => TestIndexer5(a, out int z);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer5(Span<byte> data, out int z)
        {
            byte x = 0;
            z = -1;

            // Write to z here should not be able to modify
            // the span.
            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
                z = idx;
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte Indexer6() => TestIndexer6(a, sink);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestIndexer6(Span<byte> data, Sink s)
        {
            byte x = 0;

            // Write to s.i here should not be able to modify
            // the span.
            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
                s.i = 0;
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte ReadOnlyIndexer1() => TestReadOnlyIndexer1(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestReadOnlyIndexer1(ReadOnlySpan<byte> data)
        {
            int length = data.Length;
            byte x = 0;

            for (var idx = 0; idx < length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination")]
        public byte ReadOnlyIndexer2() => TestReadOnlyIndexer2(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestReadOnlyIndexer2(ReadOnlySpan<byte> data)
        {
            byte x = 0;

            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination w/ writes")]
        public byte WriteViaIndexer1() => TestWriteViaIndexer1(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestWriteViaIndexer1(Span<byte> data)
        {
            byte q = data[0];

            for (var idx = 1; idx < data.Length; idx++)
            {
                data[0] ^= data[idx];
            }

            byte x = data[0];
            data[0] = q;

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Indexer in-loop bounds check elimination w/ writes")]
        public byte WriteViaIndexer2() => TestWriteViaIndexer2(a, 0, a.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestWriteViaIndexer2(Span<byte> data, int start, int end)
        {
            byte x = 0;

            for (var idx = start; idx < end; idx++)
            {
                // Bounds checks are redundant
                byte b = data[idx];
                x ^= b;
                data[idx] = b;
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Span known size bounds check elimination")]
        public byte KnownSizeArray() => TestKnownSizeArray(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestKnownSizeArray(Span<byte> data)
        {
            byte x = 0;
            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Span known size bounds check elimination")]
        public byte KnownSizeCtor() => TestKnownSizeCtor(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestKnownSizeCtor(byte[] a)
        {
            Span<byte> data = new Span<byte>(a, 0, 1024);

            byte x = 0;
            for (var idx = 0; idx < data.Length; idx++)
            {
                x ^= data[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Span known size bounds check elimination")]
        public byte KnownSizeCtor2() => TestKnownSizeCtor2(a);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestKnownSizeCtor2(byte[] a)
        {
            Span<byte> data1 = new Span<byte>(a, 0, 512);
            Span<byte> data2 = new Span<byte>(a, 512, 512);
            byte x = 0;

            for (var idx = 0; idx < data1.Length; idx++)
            {
                x ^= data1[idx];
            }
            for (var idx = 0; idx < data2.Length; idx++)
            {
                x ^= data2[idx];
            }

            return x;
        }

        [Benchmark]
        [BenchmarkCategory("Same index in-loop redundant bounds check elimination")]
        public byte SameIndex1() => TestSameIndex1(a, 0, a.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestSameIndex1(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;

            for (var idx = start; idx < end; idx++)
            {
                x ^= data[idx];
                y ^= data[idx];
            }

            byte t = (byte)(y ^ x ^ y);

            return t;
        }

        [Benchmark]
        [BenchmarkCategory("Same index in-loop redundant bounds check elimination")]
        public byte SameIndex2() => TestSameIndex2(a, ref a[0], 0, a.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestSameIndex2(Span<byte> data, ref byte b, int start, int end)
        {
            byte x = 0;
            byte y = 0;
            byte ye = 121;
            byte q = data[0];

            for (var idx = start; idx < end; idx++)
            {
                // Bounds check is redundant, but values are not CSEs.
                x ^= data[idx];
                b = 1;
                y ^= data[idx];
            }

            byte t = (byte)(y ^ x ^ ye);
            data[0] = q;

            return t;
        }

        [Benchmark]
        [BenchmarkCategory("Covered index in-loop redundant bounds check elimination")]
        public byte CoveredIndex1() => TestCoveredIndex1(a, 0, a.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestCoveredIndex1(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;

            for (var idx = start; idx < end - 100; idx++)
            {
                x ^= data[idx + 100];
                y ^= data[idx];
            }

            for (var idx = end - 100; idx < end; idx++)
            {
                y ^= data[idx];
                x ^= data[idx - 100];
            }

            byte r = (byte)(x ^ y ^ x);

            return r;
        }

        [Benchmark]
        [BenchmarkCategory("Covered index in-loop redundant bounds check elimination")]
        public byte CoveredIndex2() => TestCoveredIndex2(a, 0, a.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestCoveredIndex2(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;

            for (var idx = start; idx < end; idx++)
            {
                x ^= data[idx];

                if (idx != 100)
                {
                    // Should be able to eliminate this bounds check
                    y ^= data[0];
                }
            }

            byte r = (byte)(y ^ x ^ y);

            return r;
        }

        [Benchmark]
        [BenchmarkCategory("Covered index in-loop redundant bounds check elimination")]
        public byte CoveredIndex3() => TestCoveredIndex3(a, 0, a.Length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte TestCoveredIndex3(Span<byte> data, int start, int end)
        {
            byte x = 0;
            byte y = 0;
            byte z = 0;

            for (var idx = start; idx < end; idx++)
            {
                x ^= data[idx];

                if (idx != 100)
                {
                    y ^= data[50];
                    // Should be able to eliminate this bounds check
                    z ^= data[25];
                }
            }

            byte r = (byte)(z ^ y ^ x ^ y ^ z);

            return r;
        }

        static byte[] GetData(int size)
        {
            byte[] data = new byte[size];
            SetData(data);
            return data;
        }

        static void SetData(byte[] data)
        {
            Random Rnd = new Random(42);
            Rnd.NextBytes(data);
        }
    }
}
