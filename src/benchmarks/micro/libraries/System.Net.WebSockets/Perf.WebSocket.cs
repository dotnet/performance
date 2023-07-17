// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.WebSockets.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class SocketSendReceivePerfTest
    {
        private WebSocket _client;
        private WebSocket _server;
        private Memory<byte> _buffer = new byte[1];

        [GlobalSetup]
        public void Setup()
        {
            using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);
            
            var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(listener.LocalEndPoint);
            Socket server = listener.Accept();

            _client = WebSocket.CreateFromStream(new NetworkStream(client, ownsSocket: true), isServer: false, subProtocol: null, Timeout.InfiniteTimeSpan);
            _server = WebSocket.CreateFromStream(new NetworkStream(server, ownsSocket: true), isServer: true, subProtocol: null, Timeout.InfiniteTimeSpan);
        }

        [Benchmark]
        public async Task SendReceive()
        {
            await _client.SendAsync(_buffer, WebSocketMessageType.Binary, endOfMessage: true, default);
            await _server.ReceiveAsync(_buffer, default);
        }

        [Benchmark]
        [MemoryRandomization]
        public async Task ReceiveSend()
        {
            ValueTask<ValueWebSocketReceiveResult> read = _server.ReceiveAsync(_buffer, default);
            await _client.SendAsync(_buffer, WebSocketMessageType.Binary, endOfMessage: true, default);
            await read;
        }
    }
}