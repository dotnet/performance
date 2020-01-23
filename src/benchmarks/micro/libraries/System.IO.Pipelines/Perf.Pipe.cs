// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Threading;
using System.Threading.Tasks;
using MicroBenchmarks;

namespace System.IO.Pipelines.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Pipe
    {
        private const int InnerIterationCount = 10_000;

        private Pipe _pipe;
        private CancellationTokenSource _cts;
        private byte[] _data;

        [GlobalSetup(Target = nameof(SyncReadAsync))]
        public async Task Setup_SyncReadAsync()
        {
            _pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));

            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 0, 0, 0, 0 }));
            await _pipe.Writer.FlushAsync();
        }

        [Benchmark]
        public async Task SyncReadAsync()
        {
            PipeWriter writer = _pipe.Writer;
            PipeReader reader = _pipe.Reader;

            for (int i = 0; i < InnerIterationCount; i++)
            {
                ReadResult result = await reader.ReadAsync();
                reader.AdvanceTo(result.Buffer.Start);
            }
        }

        [GlobalSetup(Target = nameof(ReadAsync))]
        public void Setup_ReadAsync()
        {
            _pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            _data = new byte[] { 0 };
        }

        [Benchmark]
        public async Task ReadAsync()
        {
            PipeWriter writer = _pipe.Writer;
            PipeReader reader = _pipe.Reader;

            for (int i = 0; i < InnerIterationCount; i++)
            {
                ValueTask<ReadResult> task = reader.ReadAsync();

                await writer.WriteAsync(_data);
                await writer.FlushAsync();

                ReadResult result = await task;
                reader.AdvanceTo(result.Buffer.End);
            }
        }

        [GlobalSetup(Target = nameof(SyncReadAsyncWithCancellationToken))]
        public async Task Setup_SyncReadAsyncWithCancellationToken()
        {
            _pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            _cts = new CancellationTokenSource();

            await _pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 0, 0, 0, 0 }));
            await _pipe.Writer.FlushAsync();
        }

        [Benchmark]
        public async Task SyncReadAsyncWithCancellationToken()
        {
            PipeWriter writer = _pipe.Writer;
            PipeReader reader = _pipe.Reader;

            for (int i = 0; i < InnerIterationCount; i++)
            {
                ReadResult result = await reader.ReadAsync(_cts.Token);
                reader.AdvanceTo(result.Buffer.Start);
            }
        }

        [GlobalSetup(Target = nameof(ReadAsyncWithCancellationToken))]
        public void Setup_ReadAsyncWithCancellationToken()
        {
            _pipe = new Pipe(new PipeOptions(pool: null, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
            _data = new byte[] { 0 };
            _cts = new CancellationTokenSource();
        }

        [Benchmark]
        public async Task ReadAsyncWithCancellationToken()
        {
            PipeWriter writer = _pipe.Writer;
            PipeReader reader = _pipe.Reader;

            for (int i = 0; i < InnerIterationCount; i++)
            {
                ValueTask<ReadResult> task = reader.ReadAsync(_cts.Token);

                await writer.WriteAsync(_data);
                await writer.FlushAsync();

                ReadResult result = await task;
                reader.AdvanceTo(result.Buffer.End);
            }
        }
    }
}
