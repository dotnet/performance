// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// following benchmarks consume .NET Core 2.1 APIs and are disabled for other frameworks in .csproj file

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.Sockets.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class SocketSendReceivePerfTest
    {
        const int MaximumWait = 1000;
        private const int InnerIterationCount = 10_000;

        private Socket _listener, _client, _server;
        private Socket _udpClient1, _udpClient2, _udpServer;

        [Benchmark(OperationsPerInvoke = 1000)]
        public async Task ConnectAcceptAsync()
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            for (int i = 0; i < 1000; i++)
            {
                using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await client.ConnectAsync(listener.LocalEndPoint);
                using var server = await listener.AcceptAsync();
            }
        }

        [Benchmark]
        public async Task SendAsyncThenReceiveAsync_Task()
        {
            Socket client = _client, server = _server;
            
            ReadOnlyMemory<byte> clientBuffer = new byte[1];
            Memory<byte> serverBuffer = new byte[1];
            
            for (int i = 0; i < InnerIterationCount; i++)
            {
                await client.SendAsync(clientBuffer, SocketFlags.None);
                await server.ReceiveAsync(serverBuffer, SocketFlags.None);
            }
        }

        [Benchmark]
        public async Task ReceiveAsyncThenSendAsync_Task()
        {
            Socket client = _client, server = _server;
            
            ReadOnlyMemory<byte> clientBuffer = new byte[1];
            Memory<byte> serverBuffer = new byte[1];
            
            for (int i = 0; i < InnerIterationCount; i++)
            {
                ValueTask<int> r = server.ReceiveAsync(serverBuffer, SocketFlags.None);
                await client.SendAsync(clientBuffer, SocketFlags.None);
                await r;
            }
        }

        [Benchmark]
        public async Task SendAsyncThenReceiveAsync_SocketAsyncEventArgs()
        {
            Socket client = _client, server = _server;
            
            var clientSaea = new AwaitableSocketAsyncEventArgs();
            var serverSaea = new AwaitableSocketAsyncEventArgs();
            
            clientSaea.SetBuffer(new byte[1], 0, 1);
            serverSaea.SetBuffer(new byte[1], 0, 1);
            
            for (int i = 0; i < InnerIterationCount; i++)
            {
                if (client.SendAsync(clientSaea))
                {
                    await clientSaea;
                }
                if (server.ReceiveAsync(serverSaea))
                {
                    await serverSaea;
                }
            }
        }

        [Benchmark]
        public async Task ReceiveAsyncThenSendAsync_SocketAsyncEventArgs()
        {
            Socket client = _client, server = _server;

            var clientSaea = new AwaitableSocketAsyncEventArgs();
            var serverSaea = new AwaitableSocketAsyncEventArgs();

            clientSaea.SetBuffer(new byte[1], 0, 1);
            serverSaea.SetBuffer(new byte[1], 0, 1);

            for (int i = 0; i < InnerIterationCount; i++)
            {
                bool pendingServer = server.ReceiveAsync(serverSaea);

                if (client.SendAsync(clientSaea))
                {
                    await clientSaea;
                }

                if (pendingServer)
                {
                    await serverSaea;
                }
            }
        }

        [Benchmark]
        public async Task ReceiveFromAsyncThenSendToAsync_Task()
        {
            ReadOnlyMemory<byte> clientBuffer = new byte[1];
            Memory<byte> serverBuffer = new byte[1];

            EndPoint ep = new IPEndPoint(IPAddress.None, 0);

            var ct = new CancellationTokenSource(MaximumWait * InnerIterationCount);

            for (int i = 0; i < InnerIterationCount; i++)
            {
                ValueTask<SocketReceiveFromResult> r = _udpServer.ReceiveFromAsync(serverBuffer, SocketFlags.None, ep, ct.Token);
                await _udpClient1.SendToAsync(clientBuffer, SocketFlags.None, _udpServer.LocalEndPoint);
                await r;
                r = _udpServer.ReceiveFromAsync(serverBuffer, SocketFlags.None, ep, ct.Token);
                await _udpClient2.SendToAsync(clientBuffer, SocketFlags.None, _udpServer.LocalEndPoint);
                await r;
            }
        }

        [Benchmark]
        public async Task ReceiveFromAsyncThenSendToAsync_SocketAddress()
        {
            ReadOnlyMemory<byte> clientBuffer = new byte[1];
            Memory<byte> serverBuffer = new byte[1];

            SocketAddress serverSa = _udpServer.LocalEndPoint.Serialize();
            SocketAddress receivedSa = new SocketAddress(_udpServer.AddressFamily);

            var ct = new CancellationTokenSource(MaximumWait * InnerIterationCount);

            for (int i = 0; i < InnerIterationCount; i++)
            {
                ValueTask<int> r = _udpServer.ReceiveFromAsync(serverBuffer, SocketFlags.None, receivedSa, ct.Token);
                await _udpClient1.SendToAsync(clientBuffer, SocketFlags.None, serverSa);
                await r;
                r = _udpServer.ReceiveFromAsync(serverBuffer, SocketFlags.None, receivedSa, ct.Token);
                await _udpClient2.SendToAsync(clientBuffer, SocketFlags.None, serverSa);
                await r;
            }
        }

        [Benchmark]
        public void SendToThenReceiveFrom()
        {
            Socket client = _udpClient1, server = _udpServer;

            ReadOnlySpan<byte> clientBuffer = new byte[1];
            Span<byte> serverBuffer = new byte[1];

            EndPoint ep = new IPEndPoint(IPAddress.None, 0);
            for (int i = 0; i < InnerIterationCount; i++)
            {
                _udpClient1.SendTo(clientBuffer, SocketFlags.None, _udpServer.LocalEndPoint);
                _udpServer.ReceiveFrom(serverBuffer, SocketFlags.None, ref ep);
                _udpClient2.SendTo(clientBuffer, SocketFlags.None, _udpServer.LocalEndPoint);
                _udpServer.ReceiveFrom(serverBuffer, SocketFlags.None, ref ep);
            }
        }

        [GlobalSetup]
        public async Task OpenLoopbackConnectionAsync()
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _listener.Listen(1);

            Task<Socket> acceptTask = _listener.AcceptAsync();
            Task connectTask = _client.ConnectAsync(_listener.LocalEndPoint);

            await await Task.WhenAny(acceptTask, connectTask);
            await Task.WhenAll(acceptTask, connectTask);

            _server = await acceptTask;

            _udpClient1 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpClient1.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _udpClient2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpClient2.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpServer.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _udpServer.ReceiveTimeout = MaximumWait;
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _server.Dispose();
            _client.Dispose();
            _listener.Dispose();
            _udpClient1.Dispose();
            _udpClient2.Dispose();
            _udpServer.Dispose();
        }

        internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
        {
            private static readonly Action s_completedSentinel = () => { };
            private Action _continuation;

            public AwaitableSocketAsyncEventArgs()
            {
                Completed += delegate
                {
                    Action c = _continuation;
                    if (c != null)
                    {
                        c();
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null)?.Invoke();
                    }
                };
            }

            public AwaitableSocketAsyncEventArgs GetAwaiter() => this;

            public bool IsCompleted => _continuation != null;

            public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

            public void OnCompleted(Action continuation)
            {
                if (ReferenceEquals(_continuation, s_completedSentinel) ||
                    ReferenceEquals(Interlocked.CompareExchange(ref _continuation, continuation, null), s_completedSentinel))
                {
                    Task.Run(continuation);
                }
            }

            public int GetResult()
            {
                _continuation = null;
                if (SocketError != SocketError.Success)
                {
                    throw new SocketException((int)SocketError);
                }
                return BytesTransferred;
            }
        }
    }
}
