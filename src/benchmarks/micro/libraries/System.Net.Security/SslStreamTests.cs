// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;  
using System.IO;
using System.IO.Pipes;
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
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public partial class SslStreamTests
    {
        private readonly Barrier _twoParticipantBarrier = new Barrier(2);
        private static readonly X509Certificate2 _cert = Test.Common.Configuration.Certificates.GetServerCertificate();
        private static readonly X509Certificate2 _clientCert = Test.Common.Configuration.Certificates.GetClientCertificate();
        private static readonly X509Certificate2 _ec256Cert = Test.Common.Configuration.Certificates.GetEC256Certificate();
        private static readonly X509Certificate2 _ec512Cert = Test.Common.Configuration.Certificates.GetEC512Certificate();
        private static readonly X509Certificate2 _rsa2048Cert = Test.Common.Configuration.Certificates.GetRSA2048Certificate();
        private static readonly X509Certificate2 _rsa4096Cert = Test.Common.Configuration.Certificates.GetRSA4096Certificate();

        private readonly byte[] _clientBuffer = new byte[1], _serverBuffer = new byte[1];
        private readonly byte[] _largeClientBuffer = new byte[4096], _largeServerBuffer = new byte[4096];

        private SslStream _sslClient, _sslServer;       // used for read/write tests
        private NetworkStream _clientIPv4, _serverIPv4; // used for handshake tests
        private NetworkStream _clientIPv6, _serverIPv6; // used for handshake tests
        private PipeStream _clientPipe, _serverPipe;    // used for handshake tests

        [GlobalSetup]
        public void Setup()
        {
            string pipeName = "SetupTlsHandshakePipe";
            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                // Create an SslStream pair
                var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(listener.LocalEndPoint);
                Socket server = listener.Accept();

                _sslClient = new SslStream(new NetworkStream(client, ownsSocket: true), leaveInnerStreamOpen: false, delegate { return true; });
                _sslServer = new SslStream(new NetworkStream(server, ownsSocket: true), leaveInnerStreamOpen: false, delegate { return true; });
                Task.WaitAll(
                    _sslClient.AuthenticateAsClientAsync("localhost", null, SslProtocols.None, checkCertificateRevocation: false),
                    _sslServer.AuthenticateAsServerAsync(_cert, clientCertificateRequired: false, SslProtocols.None, checkCertificateRevocation: false));

                // Create a IPv4 pair
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(listener.LocalEndPoint);
                server = listener.Accept();
                _clientIPv4 = new NetworkStream(client, ownsSocket: true);
                _serverIPv4 = new NetworkStream(server, ownsSocket: true);
            }

            // Create IPv6 TCP pair.
            using (var listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                listener.Listen(1);

                var client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(listener.LocalEndPoint);
                Socket server = listener.Accept();

                _clientIPv6 = new NetworkStream(client, ownsSocket: true);
                _serverIPv6 = new NetworkStream(server, ownsSocket: true);
            }

            // Create PIPE Pair.
            var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            Task.WaitAll(pipeServer.WaitForConnectionAsync(), pipeClient.ConnectAsync());
            _serverPipe = pipeServer;
            _clientPipe = pipeClient;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _sslClient.Dispose();
            _sslServer.Dispose();
            _clientIPv4.Dispose();
            _serverIPv4.Dispose();
            _clientIPv6.Dispose();
            _serverIPv6.Dispose();
            _clientPipe.Dispose();
            _serverPipe.Dispose();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultHandshakeIPv4Async() => DefaultHandshake(_clientIPv4, _serverIPv4);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultHandshakeIPv6Async() => DefaultHandshake(_clientIPv6, _serverIPv6);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultMutualHandshakeIPv4Async() => DefaultHandshake(_clientIPv4, _serverIPv4, requireClientCert: true);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultMutualHandshakeIPv6Async() => DefaultHandshake(_clientIPv6, _serverIPv6, requireClientCert: true);

        [Benchmark]
        [OperatingSystemsFilter(allowed: true, platforms: OS.Linux)]    // Not supported on Windows at the moment.
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultHandshakePipeAsync() => DefaultHandshake(_clientPipe, _serverPipe);

        private async Task DefaultHandshake(Stream client, Stream server, bool requireClientCert = false)
        {
            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = SslProtocols.None,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                TargetHost = "loopback",
                ClientCertificates = requireClientCert ? new X509CertificateCollection() { _clientCert } : null,
            };

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = SslProtocols.None,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ServerCertificate = _cert,
                ClientCertificateRequired = requireClientCert,
            };

            using (var sslClient = new SslStream(client, leaveInnerStreamOpen: true, delegate { return true; }))
            using (var sslServer = new SslStream(server, leaveInnerStreamOpen: true, delegate { return true; }))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await Task.WhenAll(
                    sslClient.AuthenticateAsClientAsync(clientOptions, cts.Token),
                    sslServer.AuthenticateAsServerAsync(serverOptions, cts.Token));
                if ((int)sslClient.SslProtocol > (int)SslProtocols.Tls12)
                {
                    // In Tls1.3 part of handshake happens with data exchange.
                    await sslClient.WriteAsync(_clientBuffer, cts.Token);
#pragma warning disable CA2022 // Avoid inexact read
                    await sslServer.ReadAsync(_serverBuffer, cts.Token);
                    await sslServer.WriteAsync(_serverBuffer, cts.Token);
                    await sslClient.ReadAsync(_clientBuffer, cts.Token);
#pragma warning restore CA2022
                }
            }
        }

        private static async Task HandshakeAsync(X509Certificate certificate, SslProtocols sslProtocol)
        {
            RemoteCertificateValidationCallback clientRemoteCallback = new RemoteCertificateValidationCallback(delegate { return true; });
            (Stream clientStream, Stream serverStream) = ConnectedStreams.CreateBidirectional(initialBufferSize: 4096, maxBufferSize: int.MaxValue);
            SslClientAuthenticationOptions clientOptions = new SslClientAuthenticationOptions
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = sslProtocol,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                TargetHost = Guid.NewGuid().ToString(),
                RemoteCertificateValidationCallback = clientRemoteCallback
            };

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = sslProtocol,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ServerCertificate = certificate
            };

            using (var sslClient = new SslStream(clientStream))
            using (var sslServer = new SslStream(serverStream))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                await Task.WhenAll(
                        sslClient.AuthenticateAsClientAsync(clientOptions, cts.Token),
                        sslServer.AuthenticateAsServerAsync(serverOptions, cts.Token));

                byte[] clientBuffer = new byte[1], serverBuffer = new byte[1];
                await sslClient.WriteAsync(clientBuffer, cts.Token);
#pragma warning disable CA2022 // Avoid inexact read
                await sslServer.ReadAsync(serverBuffer, cts.Token);
                await sslServer.WriteAsync(serverBuffer, cts.Token);
                await sslClient.ReadAsync(clientBuffer, cts.Token);
#pragma warning restore CA2022
            }
        }

        private const int ReadWriteIterations = 50_000;

        [Benchmark(OperationsPerInvoke = ReadWriteIterations)]
        [BenchmarkCategory(Categories.NoAOT)]
        public async Task WriteReadAsync()
        {
            Memory<byte> clientBuffer = _clientBuffer;
            Memory<byte> serverBuffer = _serverBuffer;
            for (int i = 0; i < ReadWriteIterations; i++)
            {
                await _sslClient.WriteAsync(clientBuffer, default);
#pragma warning disable CA2022 // Avoid inexact read
                await _sslServer.ReadAsync(serverBuffer, default);
#pragma warning restore CA2022
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public async Task LargeWriteReadAsync()
        {
            Memory<byte> clientBuffer = _largeClientBuffer;
            Memory<byte> serverBuffer = _largeServerBuffer;
            await _sslClient.WriteAsync(clientBuffer, default);
#pragma warning disable CA2022 // Avoid inexact read
            await _sslServer.ReadAsync(serverBuffer, default);
#pragma warning restore CA2022
        }

        [Benchmark(OperationsPerInvoke = ReadWriteIterations)]
        [BenchmarkCategory(Categories.NoAOT)]
        public async Task ReadWriteAsync()
        {
            Memory<byte> clientBuffer = _clientBuffer;
            Memory<byte> serverBuffer = _serverBuffer;
            for (int i = 0; i < ReadWriteIterations; i++)
            {
                ValueTask<int> read = _sslServer.ReadAsync(serverBuffer, default);
                await _sslClient.WriteAsync(clientBuffer, default);
                await read;
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public async Task LargeReadWriteAsync()
        {
            Memory<byte> clientBuffer = _largeClientBuffer;
            Memory<byte> serverBuffer = _largeServerBuffer;
            ValueTask<int> read = _sslServer.ReadAsync(serverBuffer, default);
            await _sslClient.WriteAsync(clientBuffer, default);
            await read;
        }

        private const int ConcurrentReadWriteIterations = 50_000;

        [Benchmark(OperationsPerInvoke = ConcurrentReadWriteIterations)]
        [BenchmarkCategory(Categories.NoAOT)]
        public async Task ConcurrentReadWrite()
        {
            Memory<byte> buffer1 = _clientBuffer;
            Memory<byte> buffer2 = _serverBuffer;

#pragma warning disable CA2022 // Avoid inexact read
            Task other = Task.Run(async delegate
            {
                _twoParticipantBarrier.SignalAndWait();
                for (int i = 0; i < ConcurrentReadWriteIterations; i++)
                {
                    await _sslServer.WriteAsync(buffer1, default);
                    await _sslClient.ReadAsync(buffer1, default);
                }
            });

            _twoParticipantBarrier.SignalAndWait();
            for (int i = 0; i < ConcurrentReadWriteIterations; i++)
            {
                await _sslClient.WriteAsync(buffer2, default);
                await _sslServer.ReadAsync(buffer2, default);
            }
#pragma warning restore CA2022 

            await other;
        }

        private const int ConcurrentReadWriteLargeBufferIterations = 10_000;

        [Benchmark(OperationsPerInvoke = ConcurrentReadWriteLargeBufferIterations)]
        [BenchmarkCategory(Categories.NoAOT)]
        public async Task ConcurrentReadWriteLargeBuffer()
        {
            Memory<byte> buffer1 = _largeClientBuffer;
            Memory<byte> buffer2 = _largeServerBuffer;

            Task other = Task.Run(async delegate
            {
                _twoParticipantBarrier.SignalAndWait();
                for (int i = 0; i < ConcurrentReadWriteLargeBufferIterations; i++)
                {
                    await _sslServer.WriteAsync(buffer1, default);

                    int totalRead = 0;
                    Memory<byte> buff = buffer1;
                    while (totalRead < buffer1.Length)
                    {
                        int bytesRead = await _sslClient.ReadAsync(buff, default);
                        totalRead += bytesRead;
                        buff = buff.Slice(bytesRead);
                    }
                }
            });

            _twoParticipantBarrier.SignalAndWait();
            for (int i = 0; i < ConcurrentReadWriteLargeBufferIterations; i++)
            {
                await _sslClient.WriteAsync(buffer2, default);

                int totalRead = 0;
                Memory<byte> buff = buffer2;
                while (totalRead < buffer2.Length)
                {
                    int bytesRead = await _sslServer.ReadAsync(buff, default);
                    totalRead += bytesRead;
                    buff = buff.Slice(bytesRead);
                }
            }

            await other;
        }
    }
}
