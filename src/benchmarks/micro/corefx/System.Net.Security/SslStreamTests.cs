// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.Security.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class SslStreamTests
    {
        private readonly Barrier _twoParticipantBarrier = new Barrier(2);
        private readonly X509Certificate2 _cert = Test.Common.Configuration.Certificates.GetServerCertificate();
        private readonly byte[] _clientBuffer = new byte[1], _serverBuffer = new byte[1];
        private readonly byte[] _largeClientBuffer = new byte[4096], _largeServerBuffer = new byte[4096];

        private NetworkStream _client, _server; // used for handshake tests
        private SslStream _sslClient, _sslServer; // used for read/write tests

        [GlobalSetup]
        public void Setup()
        {
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

                // Create a non-SslStream pair
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(listener.LocalEndPoint);
                server = listener.Accept();
                _client = new NetworkStream(client, ownsSocket: true);
                _server = new NetworkStream(server, ownsSocket: true);
            }
        }

        [Benchmark]
        public async Task HandshakeAsync()
        {
            using (var sslClient = new SslStream(_client, leaveInnerStreamOpen: true, delegate { return true; }))
            using (var sslServer = new SslStream(_server, leaveInnerStreamOpen: true, delegate { return true; }))
            {
                await Task.WhenAll(
                    sslClient.AuthenticateAsClientAsync("localhost", null, SslProtocols.None, checkCertificateRevocation: false),
                    sslServer.AuthenticateAsServerAsync(_cert, clientCertificateRequired: false, SslProtocols.None, checkCertificateRevocation: false));

                // Workaround for corefx#37765
                await sslServer.WriteAsync(_serverBuffer, default);
                await sslClient.ReadAsync(_clientBuffer, default);
            }
        }

        private const int ReadWriteIterations = 50_000;

        [Benchmark(OperationsPerInvoke = ReadWriteIterations)]
        public async Task WriteReadAsync()
        {
            Memory<byte> clientBuffer = _clientBuffer;
            Memory<byte> serverBuffer = _serverBuffer;
            for (int i = 0; i < ReadWriteIterations; i++)
            {
                await _sslClient.WriteAsync(clientBuffer, default);
                await _sslServer.ReadAsync(serverBuffer, default);
            }
        }

        [Benchmark(OperationsPerInvoke = ReadWriteIterations)]
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

        private const int ConcurrentReadWriteIterations = 50_000;

        [Benchmark(OperationsPerInvoke = ConcurrentReadWriteIterations)]
        public async Task ConcurrentReadWrite()
        {
            Memory<byte> buffer1 = _clientBuffer;
            Memory<byte> buffer2 = _serverBuffer;

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

            await other;
        }

        private const int ConcurrentReadWriteLargeBufferIterations = 10_000;

        [Benchmark(OperationsPerInvoke = ConcurrentReadWriteLargeBufferIterations)]
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
