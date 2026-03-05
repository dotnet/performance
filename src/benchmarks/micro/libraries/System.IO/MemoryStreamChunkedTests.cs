// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    /// <summary>
    /// Chunked / buffered operations. Fixed 64KB stream, chunk size controls call count.
    /// Read/Write variants loop over the stream in ChunkSize increments.
    /// CopyTo / CopyToAsync pass ChunkSize as the bufferSize parameter.
    /// </summary>
    [BenchmarkCategory(Categories.Libraries)]
    public class MemoryStreamChunkedTests
    {
        private const int StreamSize = 65536;

        private MemoryStream _stream;
        private byte[] _buffer;

        [Params(
            1,      // 65536 calls, per-call overhead dominates
            4096)]  // 16 calls, default StreamReader/BufferedStream buffer size
        public int ChunkSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            byte[] backing = new byte[StreamSize];
            new Random(42).NextBytes(backing);
            _buffer = new byte[ChunkSize];
            _stream = new MemoryStream(backing, writable: true);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _stream.Dispose();
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
        [BenchmarkCategory(Categories.NoWASM)]
        [MemoryRandomization]
        public async Task<int> ReadAsyncMemory()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int count = 0;
            int n;
            while ((n = await s.ReadAsync(_buffer, CancellationToken.None)) > 0)
                count += n;
            return count;
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteByteArray()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int remaining = StreamSize;
            while (remaining > 0)
            {
                int chunk = Math.Min(_buffer.Length, remaining);
                s.Write(_buffer, 0, chunk);
                remaining -= chunk;
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteSpan()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int remaining = StreamSize;
            while (remaining > 0)
            {
                int chunk = Math.Min(_buffer.Length, remaining);
                s.Write(_buffer.AsSpan(0, chunk));
                remaining -= chunk;
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [MemoryRandomization]
        public async Task WriteAsyncMemory()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            int remaining = StreamSize;
            while (remaining > 0)
            {
                int chunk = Math.Min(_buffer.Length, remaining);
                await s.WriteAsync(_buffer.AsMemory(0, chunk), CancellationToken.None);
                remaining -= chunk;
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void CopyToWithBufferSize()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            s.CopyTo(Stream.Null, ChunkSize);
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [MemoryRandomization]
        public async Task CopyToAsyncWithBufferSize()
        {
            MemoryStream s = _stream;
            s.Position = 0;
            await s.CopyToAsync(Stream.Null, ChunkSize);
        }
    }
}
