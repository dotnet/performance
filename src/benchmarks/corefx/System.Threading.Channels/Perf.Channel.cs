// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Threading.Channels.Tests
{
    public class UnboundedChannelPerfTests : PerfTests
    {
        public sealed override Channel<int> CreateChannel() => Channel.CreateUnbounded<int>();
    }

    public class SpscUnboundedChannelPerfTests : PerfTests
    {
        public sealed override Channel<int> CreateChannel() => Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
    }

    public class BoundedChannelPerfTests : PerfTests
    {
        public sealed override Channel<int> CreateChannel() => Channel.CreateBounded<int>(10);
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public abstract class PerfTests
    {
        private Channel<int> _channel, _channel1, _channel2;
        private ChannelReader<int> _reader;
        private ChannelWriter<int> _writer;

        public abstract Channel<int> CreateChannel();

        [GlobalSetup(Target = nameof(TryWriteThenTryRead) + "," + nameof(WriteAsyncThenReadAsync) + "," + nameof(ReadAsyncThenWriteAsync))]
        public void SingleChannelSetup()
        {
            _channel = CreateChannel();
            _reader = _channel.Reader;
            _writer = _channel.Writer;
        }

        [Benchmark]
        public void TryWriteThenTryRead()
        {
            ChannelReader<int> reader = _reader;
            ChannelWriter<int> writer = _writer;

            for (int i = 0; i < 1_000_000; i++)
            {
                writer.TryWrite(i);
                reader.TryRead(out _);
            }
        }

        [Benchmark]
        public async Task WriteAsyncThenReadAsync()
        {
            ChannelReader<int> reader = _reader;
            ChannelWriter<int> writer = _writer;

            for (int i = 0; i < 1_000_000; i++)
            {
                await writer.WriteAsync(i);
                await reader.ReadAsync();
            }
        }

        [Benchmark]
        public async Task ReadAsyncThenWriteAsync()
        {
            ChannelReader<int> reader = _reader;
            ChannelWriter<int> writer = _writer;

            for (int i = 0; i < 1_000_000; i++)
            {
                ValueTask<int> r = reader.ReadAsync();
                await writer.WriteAsync(42);
                await r;
            }
        }

        [GlobalSetup(Target = nameof(PingPong))]
        public void SetupTwoChannels()
        {
            _channel1 = CreateChannel();
            _channel2 = CreateChannel();
        }

        [Benchmark]
        public async Task PingPong()
        {
            Channel<int> channel1 = _channel1;
            Channel<int> channel2 = _channel2;

            await Task.WhenAll(
                Task.Run(async () =>
                {
                    ChannelReader<int> reader = channel1.Reader;
                    ChannelWriter<int> writer = channel2.Writer;
                    for (int i = 0; i < 1_000_000; i++)
                    {
                        await writer.WriteAsync(i);
                        await reader.ReadAsync();
                    }
                }),
                Task.Run(async () =>
                {
                    ChannelWriter<int> writer = channel1.Writer;
                    ChannelReader<int> reader = channel2.Reader;
                    for (int i = 0; i < 1_000_000; i++)
                    {
                        await reader.ReadAsync();
                        await writer.WriteAsync(i);
                    }
                }));
        }
    }
}