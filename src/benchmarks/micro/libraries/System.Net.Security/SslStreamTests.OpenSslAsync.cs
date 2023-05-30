// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;  
using System.IO;
using System.IO.Pipes;
using System.Net.Security;
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
        private const int ConcurrentTasks = 200;
        private const int ConcurrentPipeHandshakeTasks = 50;

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentDefaultHandshakeIPv4Async() => Spawn(ConcurrentTasks, async () =>
        {
            (NetworkStream client, NetworkStream server) = ConcurrentObjectProvider.CreateIPv4Pair();
            await DefaultHandshake(client, server);
            client.Dispose();
            server.Dispose();
        });

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentDefaultHandshakeIPv6Async() => Spawn(ConcurrentTasks, async () =>
        {
            (NetworkStream client, NetworkStream server) = ConcurrentObjectProvider.CreateIPv6Pair();
            await DefaultHandshake(client, server);
            client.Dispose();
            server.Dispose();
        });

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentDefaultMutualHandshakeIPv4Async() => Spawn(ConcurrentTasks, async () =>
        {
            (NetworkStream client, NetworkStream server) = ConcurrentObjectProvider.CreateIPv4Pair();
            await DefaultHandshake(client, server, requireClientCert: true);
            client.Dispose();
            server.Dispose();
        });

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentDefaultMutualHandshakeIPv6Async() => Spawn(ConcurrentTasks, async () =>
        {
            (NetworkStream client, NetworkStream server) = ConcurrentObjectProvider.CreateIPv6Pair();
            await DefaultHandshake(client, server, requireClientCert: true);
            client.Dispose();
            server.Dispose();
        });
        
        [Benchmark]
        [OperatingSystemsFilter(allowed: true, platforms: OS.Linux)]    // Not supported on Windows at the moment.
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentDefaultHandshakePipeAsync() => Spawn(ConcurrentPipeHandshakeTasks, async () =>
        {
            (PipeStream client, PipeStream server) = ConcurrentObjectProvider.CreatePipePair();
            await DefaultHandshake(client, server);
            client.Dispose();
            server.Dispose();
        });

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentDefaultHandshakeContextIPv4Async() => Spawn(ConcurrentTasks, async () =>
        {
            (NetworkStream client, NetworkStream server) = ConcurrentObjectProvider.CreateIPv4Pair();
            await DefaultContextHandshake(client, server);
            client.Dispose();
            server.Dispose();
        });

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentDefaultHandshakeContextIPv6Async() => Spawn(ConcurrentTasks, async () =>
        {
            (NetworkStream client, NetworkStream server) = ConcurrentObjectProvider.CreateIPv6Pair();
            await DefaultContextHandshake(client, server);
            client.Dispose();
            server.Dispose();
        });
        
        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        [ArgumentsSource(nameof(TlsProtocols))]
        public Task ConcurrentHandshakeContosoAsync(SslProtocols protocol) => Spawn(ConcurrentTasks, async () => await HandshakeAsync(SslStreamTests._cert, protocol));

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentHandshakeECDSA256CertAsync(SslProtocols protocol) => Spawn(ConcurrentTasks, async () => await HandshakeAsync(SslStreamTests._ec256Cert, protocol));

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        [BenchmarkCategory(Categories.NoAOT)]
        [OperatingSystemsFilter(allowed: true, platforms: OS.Linux)]    // Not supported on Windows at the moment.
        public Task ConcurrentHandshakeECDSA512CertAsync(SslProtocols protocol) => Spawn(ConcurrentTasks, async () => await HandshakeAsync(SslStreamTests._ec512Cert, protocol));

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task ConcurrentHandshakeRSA2048CertAsync(SslProtocols protocol) => Spawn(ConcurrentTasks, async () => await HandshakeAsync(SslStreamTests._rsa2048Cert, protocol));

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        public Task ConcurrentHandshakeRSA4096CertAsync(SslProtocols protocol) => Spawn(ConcurrentTasks, async () => await HandshakeAsync(SslStreamTests._rsa4096Cert, protocol));

        private static async Task Spawn(int count, Func<Task> method)
        {
            var _tasks = new Collections.Generic.List<Task>(count);

            for(int i = 0; i < count; ++i)
            {
                _tasks.Add(Task.Run(method));
            }

            await Task.WhenAll(_tasks);
        }
    }

    internal static class ConcurrentObjectProvider
    {
        private static Socket _listenerIPv4 = null;
        private static Socket _listenerIPv6 = null;

        private const string _pipeName = "ConcurrentTlsHandshakePipe";

        private static int pipeCount = 0;

        static ConcurrentObjectProvider()
        {
            _listenerIPv4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenerIPv4.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _listenerIPv4.Listen((int)SocketOptionName.MaxConnections);

            _listenerIPv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            _listenerIPv6.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
            _listenerIPv6.Listen((int)SocketOptionName.MaxConnections);
        }

        public static Tuple<NetworkStream, NetworkStream> CreateIPv4Pair()
        {
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(_listenerIPv4.LocalEndPoint);

            var server = _listenerIPv4.Accept();

            var clientIPv4 = new NetworkStream(client, ownsSocket: true);
            var serverIPv4 = new NetworkStream(server, ownsSocket: true);

            return new Tuple<NetworkStream, NetworkStream>(clientIPv4, serverIPv4);
        }

        public static Tuple<NetworkStream, NetworkStream> CreateIPv6Pair()
        {
            var client = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(_listenerIPv6.LocalEndPoint);
            
            Socket server = _listenerIPv6.Accept();

            var clientIPv6 = new NetworkStream(client, ownsSocket: true);
            var serverIPv6 = new NetworkStream(server, ownsSocket: true);

            return new Tuple<NetworkStream, NetworkStream>(clientIPv6, serverIPv6);
        }

        public static Tuple<PipeStream, PipeStream> CreatePipePair()
        {
            var pipe = _pipeName + pipeCount++;

            var pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            
            var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

            Task.WaitAll(pipeServer.WaitForConnectionAsync(), pipeClient.ConnectAsync());

            return new Tuple<PipeStream, PipeStream>(pipeClient, pipeServer);
        }

        public static void Cleanup()
        {
            _listenerIPv4.Dispose();
            _listenerIPv6.Dispose();
        }
    }
}
