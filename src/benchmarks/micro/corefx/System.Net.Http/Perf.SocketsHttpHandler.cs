// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// following benchmarks consume .NET Core 2.1 APIs and are disabled for other frameworks in .csproj file

using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.Http.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class SocketsHttpHandlerPerfTest
    {
        // the field names start with lowercase to keep to benchmark ID! do not change it
        [Params(false, true)]
        public bool Ssl { get; set; }

        [Params(false, true)]
        public bool ChunkedResponse { get; set; }

        [Params(1, 1024, 10 * 1024 * 1024)]
        public int ResponseLength { get; set; }

        private System.Security.Cryptography.X509Certificates.X509Certificate2 _serverCert;
        private Socket _listener;
        private Task _serverTask;
        private SocketsHttpHandler _handler;
        private HttpMessageInvoker _invoker;
        private HttpRequestMessage _request;

        [GlobalSetup]
        public void Setup()
        {
            _serverCert = Test.Common.Configuration.Certificates.GetServerCertificate();
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _listener.Listen(int.MaxValue);
            string responseText =
                "HTTP/1.1 200 OK\r\n" + (ChunkedResponse ?
                $"Transfer-Encoding: chunked\r\n\r\n{ResponseLength.ToString("X")}\r\n{new string('a', ResponseLength)}\r\n0\r\n\r\n" :
                $"Content-Length: {ResponseLength}\r\n\r\n{new string('a', ResponseLength)}");
            ReadOnlyMemory<byte> responseBytes = Encoding.UTF8.GetBytes(responseText);

            _serverTask = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        using (Socket s = await _listener.AcceptAsync())
                        {
                            try
                            {
                                Stream stream = new NetworkStream(s);
                                if (Ssl)
                                {
                                    var sslStream = new SslStream(stream, false, delegate { return true; });
                                    await sslStream.AuthenticateAsServerAsync(_serverCert, false, SslProtocols.None, false);
                                    stream = sslStream;
                                }

                                using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 100))
                                {
                                    while (true)
                                    {
                                        while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) ;
                                        await stream.WriteAsync(responseBytes);
                                    }
                                }
                            }
                            catch (SocketException e) when (e.SocketErrorCode == SocketError.ConnectionAborted) { }
                        }
                    }
                }
                catch { }
            });

            var ep = (IPEndPoint)_listener.LocalEndPoint;
            var uri = new Uri($"http{(Ssl ? "s" : "")}://{ep.Address}:{ep.Port}/");
            _handler = new SocketsHttpHandler();
            _invoker = new HttpMessageInvoker(_handler);

            if (Ssl)
            {
                _handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
            }

            _request = new HttpRequestMessage(HttpMethod.Get, uri);
        }

        [Benchmark]
        public async Task Get()
        {
            var invoker = _invoker;
            var req = _request;

            using (HttpResponseMessage resp = await invoker.SendAsync(req, CancellationToken.None))
            using (Stream respStream = await resp.Content.ReadAsStreamAsync())
            {
                await respStream.CopyToAsync(Stream.Null);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _invoker.Dispose();
            _handler.Dispose();
            _listener.Dispose();
            _serverTask.GetAwaiter().GetResult();
            _serverCert.Dispose();
        }
    }
}