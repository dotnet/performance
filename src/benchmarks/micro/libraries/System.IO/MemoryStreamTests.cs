// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    /// <summary>
    /// Operations that do not receive a user buffer. Backing size controls iteration count.
    /// ReadByte / WriteByte exercise per-byte overhead; CopyTo / CopyToAsync use the
    /// default buffer size overload.
    /// </summary>
    [BenchmarkCategory(Categories.Libraries)]
    public class MemoryStreamTests
    {
        private MemoryStream _stream;

        [Params(
            1024,   // small stream
            65536)] // large stream
        public int Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            byte[] backing = new byte[Size];
            new Random(42).NextBytes(backing);
            _stream = new MemoryStream(backing, writable: true);
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
        public void WriteByte()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            for (int i = 0; i < Size; i++)
                s.WriteByte((byte)i);
        }

        [Benchmark]
        [MemoryRandomization]
        public void CopyTo()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            s.CopyTo(Stream.Null);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [MemoryRandomization]
        public async Task CopyToAsync()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            await s.CopyToAsync(Stream.Null);
        }
    }
}
