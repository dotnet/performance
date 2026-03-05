// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    [MemoryDiagnoser]
    public class MemoryStreamTests
    {
        private MemoryStream _stream;
        private byte[] _buffer;

        [Params(
            1,      // per-call overhead dominates
            4096,   // default StreamReader/BufferedStream buffer size
            65536)] // large bulk transfer
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _buffer = new byte[Size];
            new Random(42).NextBytes(_buffer);
            _stream = new MemoryStream(_buffer, writable: true);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _stream.Dispose();
        }

        [Benchmark]
        [MemoryRandomization]
        public int ReadByte()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int count = 0;
            while (s.ReadByte() != -1)
                count++;
            return count;
        }

        [Benchmark]
        [MemoryRandomization]
        public int ReadByteArray()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int count = 0;
            int n;
            while ((n = s.Read(_buffer, 0, _buffer.Length)) > 0)
                count += n;
            return count;
        }

        [Benchmark]
        [MemoryRandomization]
        public int ReadSpan()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int count = 0;
            int n;
            while ((n = s.Read(_buffer.AsSpan())) > 0)
                count += n;
            return count;
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteByte()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int i = 0;
            while (i < Size)
                s.WriteByte(_buffer[i++]);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteByteArray()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            s.Write(_buffer, 0, _buffer.Length);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteSpan()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            s.Write(_buffer.AsSpan());
        }

        [Benchmark]
        [MemoryRandomization]
        public void CopyTo()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            s.CopyTo(Stream.Null);
        }
    }
}
