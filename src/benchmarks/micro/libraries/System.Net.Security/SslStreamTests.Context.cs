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
        private SslStreamCertificateContext _context = null;

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultHandshakeContextIPv4Async() => DefaultContextHandshake(_clientIPv4, _serverIPv4);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultHandshakeContextIPv6Async() => DefaultContextHandshake(_clientIPv6, _serverIPv6);


        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultMutualHandshakeContextIPv4Async() => DefaultContextHandshake(_clientIPv4, _serverIPv4, true);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task DefaultMutualHandshakeContextIPv6Async() => DefaultContextHandshake(_clientIPv6, _serverIPv6, true);
        private async Task DefaultContextHandshake(Stream client, Stream server, bool requireClientCert = false)
        {
            if (_context == null)
            {
                _context = SslStreamCertificateContext.Create(_cert, null);
            }

            SslServerAuthenticationOptions serverOptions = new SslServerAuthenticationOptions
            {
                AllowRenegotiation = false,
                EnabledSslProtocols = SslProtocols.None,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                ServerCertificateContext = _context,
            };

            using (var sslClient = new SslStream(client, leaveInnerStreamOpen: true, delegate { return true; }))
            using (var sslServer = new SslStream(server, leaveInnerStreamOpen: true, delegate { return true; }))
            {
                await Task.WhenAll(
                    sslClient.AuthenticateAsClientAsync("localhost", requireClientCert ? new X509CertificateCollection() { _clientCert } : null, SslProtocols.None, checkCertificateRevocation: false),
                    sslServer.AuthenticateAsServerAsync(serverOptions, default));

                // In Tls1.3 part of handshake happens with data exchange.
                // To be consistent we do this extra step for all protocol versions
                await sslClient.WriteAsync(_clientBuffer, default);
#pragma warning disable CA2022 // Avoid inexact read
                await sslServer.ReadAsync(_serverBuffer, default);
                await sslServer.WriteAsync(_serverBuffer, default);
                await sslClient.ReadAsync(_clientBuffer, default);
#pragma warning restore CA2022
            }
        }

    }
}
