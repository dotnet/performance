// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// following benchmarks consume .NET Core 2.1 APIs and are disabled for other frameworks in .csproj file

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Net.Sockets.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class SocketSendReceivePerfTest
    {
        private const int InnerIterationCount = 10_000;

        private Socket _listener, _client, _server;

        [GlobalSetup]
        public void Setup() => OpenLoopbackConnectionAsync().GetAwaiter().GetResult(); // BenchmarkDotNet does not support async Setup https://github.com/dotnet/BenchmarkDotNet/issues/521

        [GlobalCleanup]
        public void Cleanup()
        {
            _server.Dispose();
            _client.Dispose();
            _listener.Dispose();
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

        private async Task OpenLoopbackConnectionAsync()
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