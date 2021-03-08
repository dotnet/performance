// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Net.Security.Tests
{
    public partial class SslStreamTests
    {
        //private const int SmallWritesIterations = 100_000_000;
        private const int SmallWritesIterations = 1_000;

        async Task Server(SslStream server)
        {
            await server.AuthenticateAsServerAsync(_cert);

            // Write 50500, 100 byte frames
            for (int i = 0; i < 500_000; i++)
            {
                await server.WriteAsync(new byte[100]);
            }
            await server.ShutdownAsync();
        }


        async Task Client(SslStream client)
        {
            await client.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
            });

            // 1MB
            var buffer = new byte[0x100000];

            do
            {
                var read = await client.ReadAsync(buffer).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                if (read == 0)
                {
                    break;
                }
            }
            while (true);
        }

        [Benchmark(OperationsPerInvoke = SmallWritesIterations)]
        public async Task SmallPipeWrites()
        {
            var pair = DuplexPipe.CreateConnectionPair(new PipeOptions(), new PipeOptions());

            var server = new SslStream(new DuplexPipeStream(pair.Application.Input, pair.Application.Output));
            var client = new SslStream(new DuplexPipeStream(pair.Transport.Input, pair.Transport.Output));

            var s1 = Server(server);
            var s2 = Client(client);
            await Task.WhenAll(s1, s2);
        }

        internal class DuplexPipe : IDuplexPipe
        {
            public DuplexPipe(PipeReader reader, PipeWriter writer)
            {
                Input = reader;
                Output = writer;
            }

            public PipeReader Input { get; }

            public PipeWriter Output { get; }

            public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
            {
                var input = new Pipe(inputOptions);
                var output = new Pipe(outputOptions);

                var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
                var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

                return new DuplexPipePair(applicationToTransport, transportToApplication);
            }

            // This class exists to work around issues with value tuple on .NET Framework
            public readonly struct DuplexPipePair
            {
                public IDuplexPipe Transport { get; }
                public IDuplexPipe Application { get; }

                public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
                {
                    Transport = transport;
                    Application = application;
                }
            }
        }

        internal class DuplexPipeStream : Stream
        {
            private readonly PipeReader _input;
            private readonly PipeWriter _output;
            private readonly bool _throwOnCancelled;
            private volatile bool _cancelCalled;

            public DuplexPipeStream(PipeReader input, PipeWriter output, bool throwOnCancelled = false)
            {
                _input = input;
                _output = output;
                _throwOnCancelled = throwOnCancelled;
            }

            public void CancelPendingRead()
            {
                _cancelCalled = true;
                _input.CancelPendingRead();
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                // ValueTask uses .GetAwaiter().GetResult() if necessary
                // https://github.com/dotnet/corefx/blob/f9da3b4af08214764a51b2331f3595ffaf162abe/src/System.Threading.Tasks.Extensions/src/System/Threading/Tasks/ValueTask.cs#L156
                return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), default).Result;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            {
                return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            {
                return ReadAsyncInternal(destination, cancellationToken);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer != null)
                {
                    _output.Write(new ReadOnlySpan<byte>(buffer, offset, count));
                }

                await _output.FlushAsync(cancellationToken);
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
            {
                _output.Write(source.Span);
                await _output.FlushAsync(cancellationToken);
            }

            public override void Flush()
            {
                FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return WriteAsync(null, 0, 0, cancellationToken);
            }

            private async ValueTask<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken)
            {
                while (true)
                {
                    var result = await _input.ReadAsync(cancellationToken);
                    var readableBuffer = result.Buffer;
                    try
                    {
                        if (_throwOnCancelled && result.IsCanceled && _cancelCalled)
                        {
                            // Reset the bool
                            _cancelCalled = false;
                            throw new OperationCanceledException();
                        }

                        if (!readableBuffer.IsEmpty)
                        {
                            // buffer.Count is int
                            var count = (int)Math.Min(readableBuffer.Length, destination.Length);
                            readableBuffer = readableBuffer.Slice(0, count);
                            readableBuffer.CopyTo(destination.Span);
                            return count;
                        }

                        if (result.IsCompleted)
                        {
                            return 0;
                        }
                    }
                    finally
                    {
                        _input.AdvanceTo(readableBuffer.End, readableBuffer.End);
                    }
                }
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                throw new NotSupportedException();
            }

            public override int EndRead(IAsyncResult asyncResult)
            {
                throw new NotSupportedException();
            }   

            public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                throw new NotSupportedException();
            }

            public override void EndWrite(IAsyncResult asyncResult)
            {
                throw new NotSupportedException();
            }
        }
    }
}
