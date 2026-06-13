// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class BinaryWriterTests
    {
        private BinaryWriter _bw;

        [GlobalSetup]
        public void Setup()
        {
            _bw = new BinaryWriter(new NullWriteStream());
        }

        [Benchmark]
        [MemoryRandomization]
        public BinaryWriter DefaultCtor() => new BinaryWriter(Stream.Null);

        [Benchmark]
        public void WriteBool()
        {
            _bw.Write(true);
        }

        [Benchmark]
        public void WriteAsciiChar()
        {
            _bw.Write('a');
        }

        [Benchmark]
        public void WriteNonAsciiChar()
        {
            _bw.Write('\u00E0');
        }

        [Benchmark]
        public void WriteUInt16()
        {
            _bw.Write((ushort)0xabcd);
        }

        [Benchmark]
        public void WriteUInt32()
        {
            _bw.Write((uint)0xdeadbeef);
        }

        [Benchmark]
        public void WriteUInt64()
        {
            _bw.Write((ulong)0xdeadbeef_aabbccdd);
        }

#if NET5_0_OR_GREATER
        [Benchmark]
        public void WriteHalf()
        {
            _bw.Write((Half)3.14);
        }
#endif

        [Benchmark]
        public void WriteSingle()
        {
            _bw.Write((float)Math.PI);
        }

        [Benchmark]
        public void WriteDouble()
        {
            _bw.Write((double)Math.PI);
        }
    }

    /// <summary>
    /// Benchmarks for BinaryWriter with a non-UTF-8 encoding, exercising the
    /// _useFastUtf8 = false code path for Write(char).
    /// </summary>
    [BenchmarkCategory(Categories.Libraries)]
    public class BinaryWriterUnicodeEncodingCharTests
    {
        private BinaryWriter _bw;

        [GlobalSetup]
        public void Setup()
        {
            _bw = new BinaryWriter(new NullWriteStream(), Encoding.Unicode);
        }

        [Benchmark]
        public void WriteAsciiChar()
        {
            _bw.Write('a');
        }

        [Benchmark]
        public void WriteNonAsciiChar()
        {
            // '\u00E0' (à) encodes to 2 bytes in UTF-8, exercising multi-byte encoding paths
            _bw.Write('\u00E0');
        }
    }
}
