// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.Security.Tests
{
    // Concurrent handshakes are the common shape for clients that fan out to a single
    // host (connection pools, HTTP/2 and HTTP/3 pool warm-up, microservice call-outs),
    // but the existing SslStream benchmarks only ever handshake one connection at a
    // time. These cover the concurrent case with and without session resumption.
    //
    // This lives in its own class deliberately: [Params] apply to every benchmark in a
    // class, so adding them to SslStreamTests would multiply its entire matrix.
    //
    // Unlike the other SslStream benchmarks this one runs over loopback sockets rather
    // than in-memory streams. That is required rather than incidental: measured over
    // in-memory streams, concurrent TLS 1.3 connections resume ~96% of the time at every
    // concurrency level, while over sockets the rate falls off sharply as concurrency
    // rises. An in-memory version measures the uncontended case whatever Concurrency says.
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class SslStreamConcurrencyTests
    {
        // Constant per invocation so that the reported per-handshake cost is directly
        // comparable across concurrency levels. Must be at least the highest Concurrency
        // value, otherwise the larger levels get clamped and silently measure the same
        // thing as the largest level that fits.
        private const int HandshakesPerInvocation = 128;

        private static readonly X509Certificate2 s_serverCertificate = Test.Common.Configuration.Certificates.GetRSA2048Certificate();
        private static readonly RemoteCertificateValidationCallback s_acceptAnyCertificate = delegate { return true; };

        private Socket _listener;
        private IPEndPoint _endPoint;
        private CancellationTokenSource _serverCts;

        public static IEnumerable<SslProtocols> Protocols()
        {
            yield return SslProtocols.Tls12;

            if (SslStreamTests.SupportsTls13)
            {
                yield return SslProtocols.Tls13;
            }
        }

        [ParamsSource(nameof(Protocols))]
        public SslProtocols Protocol { get; set; }

        // MicroBenchmarks caps a benchmark at 16 test cases, and Resume x Protocol already
        // accounts for a factor of four, so this is the most concurrency levels available:
        // sequential baseline, a typical connection pool, and two burst sizes.
        [Params(1, 8, 64, 128)]
        public int Concurrency { get; set; }

        [Params(false, true)]
        public bool Resume { get; set; }

        // Session resumption is keyed by SNI name, so the same target host has to be used
        // for every connection or nothing is ever reused.
        private string TargetHost => $"concurrent-handshake-{Protocol}.benchmark";

        [GlobalSetup]
        public async Task SetupAsync()
        {
            _serverCts = new CancellationTokenSource();
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _listener.Listen(Math.Max(Concurrency, 128));
            _endPoint = (IPEndPoint)_listener.LocalEndPoint;

            _ = Task.Run(AcceptLoopAsync);

            // Populate the session cache, then confirm resumption really happens. Without
            // this check the Resume=true case can silently measure full handshakes and
            // still look like a plausible result.
            await HandshakeAsync().ConfigureAwait(false);

            if (Resume && !await ResumesAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    $"TLS session resumption is not taking effect for {Protocol}; this benchmark would measure full handshakes instead.");
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // The CTS is deliberately not disposed: server handlers still in flight read
            // its token, and disposing it turns their teardown into unobserved exceptions.
            _serverCts.Cancel();
            _listener.Dispose();
        }

        [Benchmark(OperationsPerInvoke = HandshakesPerInvocation)]
        [BenchmarkCategory(Categories.NoAOT)]
        public async Task ConcurrentHandshake()
        {
            int remaining = HandshakesPerInvocation;

            while (remaining > 0)
            {
                int wave = Math.Min(Concurrency, remaining);
                Task[] handshakes = new Task[wave];

                for (int i = 0; i < wave; i++)
                {
                    handshakes[i] = HandshakeAsync();
                }

                await Task.WhenAll(handshakes).ConfigureAwait(false);
                remaining -= wave;
            }
        }

        private async Task AcceptLoopAsync()
        {
            while (!_serverCts.IsCancellationRequested)
            {
                Socket socket;
                try
                {
                    socket = await _listener.AcceptAsync(_serverCts.Token).ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                _ = Task.Run(() => ServeAsync(socket));
            }
        }

        private async Task ServeAsync(Socket socket)
        {
            socket.NoDelay = true;

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = Protocol,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ClientCertificateRequired = false,
                ServerCertificate = s_serverCertificate
            };

            using (var sslServer = new SslStream(new NetworkStream(socket, ownsSocket: true)))
            {
                try
                {
                    await sslServer.AuthenticateAsServerAsync(serverOptions, _serverCts.Token).ConfigureAwait(false);

                    byte[] buffer = new byte[1];
                    int read = await sslServer.ReadAsync(buffer, _serverCts.Token).ConfigureAwait(false);
                    if (read > 0)
                    {
                        await sslServer.WriteAsync(buffer.AsMemory(0, read), _serverCts.Token).ConfigureAwait(false);

                        // Leaves the close to the client so the connection is not torn down
                        // while the client is still reading its session ticket.
                        await sslServer.ReadAsync(buffer, _serverCts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception) when (!_serverCts.IsCancellationRequested)
                {
                }
            }
        }

        private Task HandshakeAsync() => HandshakeAsync(null);

        private async Task<bool> ResumesAsync()
        {
            HandshakeByteCounter counter = new HandshakeByteCounter();
            await HandshakeAsync(counter).ConfigureAwait(false);

            // A full handshake carries the server certificate; a resumed one does not, so
            // the two differ by roughly a kilobyte for any realistic certificate.
            return counter.HandshakeBytesRead < 800;
        }

        private async Task HandshakeAsync(HandshakeByteCounter counter)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = Protocol,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                TargetHost = TargetHost,
                AllowTlsResume = Resume,
                RemoteCertificateValidationCallback = s_acceptAnyCertificate
            };

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                try
                {
                    await socket.ConnectAsync(_endPoint, cts.Token).ConfigureAwait(false);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }

                Stream clientStream = new NetworkStream(socket, ownsSocket: true);

                if (counter != null)
                {
                    clientStream = counter.Wrap(clientStream);
                }

                using (var sslClient = new SslStream(clientStream))
                {
                    await sslClient.AuthenticateAsClientAsync(clientOptions, cts.Token).ConfigureAwait(false);

                    counter?.MarkHandshakeComplete();

                    // TLS 1.3 delivers its session tickets after the handshake, so without a
                    // read the client never has anything to resume from.
                    byte[] buffer = new byte[1];
                    await sslClient.WriteAsync(buffer, cts.Token).ConfigureAwait(false);
#pragma warning disable CA2022 // Avoid inexact read
                    await sslClient.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
#pragma warning restore CA2022

                    // The ticket has been ingested by this point, so the connection can be
                    // reset rather than closed gracefully. At these connection rates a
                    // graceful close leaves tens of thousands of sockets in TIME_WAIT,
                    // which risks exhausting the ephemeral port range on the test machine.
                    socket.LingerState = new LingerOption(true, 0);
                }
            }
        }

        // Used only from GlobalSetup, so the measured path stays identical to the other
        // handshake benchmarks.
        private sealed class HandshakeByteCounter
        {
            private CountingStream _stream;

            public long HandshakeBytesRead { get; private set; }

            public Stream Wrap(Stream inner) => _stream = new CountingStream(inner);

            public void MarkHandshakeComplete() => HandshakeBytesRead = _stream.BytesRead;

            private sealed class CountingStream : Stream
            {
                private readonly Stream _inner;

                public CountingStream(Stream inner) => _inner = inner;

                public long BytesRead { get; private set; }

                public override bool CanRead => _inner.CanRead;
                public override bool CanSeek => false;
                public override bool CanWrite => _inner.CanWrite;
                public override long Length => throw new NotSupportedException();
                public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
                public override void Flush() => _inner.Flush();
                public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
                public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
                public override void SetLength(long value) => throw new NotSupportedException();
                public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
                public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _inner.WriteAsync(buffer, cancellationToken);

                public override int Read(byte[] buffer, int offset, int count)
                {
                    int read = _inner.Read(buffer, offset, count);
                    BytesRead += read;
                    return read;
                }

                public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
                {
                    int read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    BytesRead += read;
                    return read;
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _inner.Dispose();
                    }
                }
            }
        }
    }
}
