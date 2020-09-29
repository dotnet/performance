// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class PipeThroughputBenchmark
    {
        private const int InnerLoopCount = 512;

        private PipeReader _reader;
        private PipeWriter _writer;

        [GlobalSetup]
        public void Setup()
        {
            var chunkLength = Length / Chunks;
            if (chunkLength > MemoryPool<byte>.Shared.MaxBufferSize)
            {
                // Parallel test will deadlock if too large (waiting for second Task to complete), so N/A that run
                throw new InvalidOperationException();
            }

            if (Length != chunkLength * Chunks)
            {
                // Test will deadlock waiting for data so N/A that run
                throw new InvalidOperationException();
            }

            var pipe = new Pipe(new PipeOptions(MemoryPool<byte>.Shared));
            _reader = pipe.Reader;
            _writer = pipe.Writer;
        }

        [Params(128, 4096)]
        public int Length { get; set; }

        [Params(1, 16)]
        public int Chunks { get; set; }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public Task Parse_ParallelAsync()
        {
            var writing = Task.Run(ParallelWriter);
            var reading = Task.Run(ParallelReader);

            return Task.WhenAll(writing, reading);
        }

        async Task ParallelWriter()
        {
            var chunks = Chunks;
            var chunkLength = Length / chunks;

            for (int i = 0; i<InnerLoopCount; i++)
            {
                for (var c = 0; c<chunks; c++)
                {
                    _writer.GetMemory(chunkLength);
                    _writer.Advance(chunkLength);
                }

                await _writer.FlushAsync();
            }
        }

        async Task ParallelReader()
        {
            long remaining = Length * InnerLoopCount;
            while (remaining != 0)
            {
                var result = await _reader.ReadAsync();
                remaining -= result.Buffer.Length;
                _reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
            }
        }

        [Benchmark(OperationsPerInvoke = InnerLoopCount)]
        public async Task Parse_SequentialAsync()
        {
            var chunks = Chunks;
            var chunkLength = Length / chunks;

            for (int i = 0; i < InnerLoopCount; i++)
            {
                for (var c = 0; c < chunks; c++)
                {
                    _writer.GetMemory(chunkLength);
                    _writer.Advance(chunkLength);
                }

                await _writer.FlushAsync();

                var result = await _reader.ReadAsync();
                _reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
            }
        }
    }
}
