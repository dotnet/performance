// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Net.Security.Tests
{
    public partial class SslStreamTests
    {
        private static readonly Task<bool> s_supportsTls13 = GetTls13SupportAsync();

        public static async IAsyncEnumerable<object[]> TlsProtocols()
        {
            yield return new object[] { SslProtocols.Tls12 };
            if (await s_supportsTls13)
            {
                yield return new object[] { SslProtocols.Tls13 };
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        [ArgumentsSource(nameof(TlsProtocols))]
        public Task HandshakeContosoAsync(SslProtocols protocol) => SslStreamTests.HandshakeAsync(SslStreamTests._cert, protocol);

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task HandshakeECDSA256CertAsync(SslProtocols protocol) => SslStreamTests.HandshakeAsync(SslStreamTests._ec256Cert, protocol);

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        [BenchmarkCategory(Categories.NoAOT)]
        [OperatingSystemsFilter(allowed: true, platforms: OS.Linux)]    // Not supported on Windows at the moment.
        public Task HandshakeECDSA512CertAsync(SslProtocols protocol) => SslStreamTests.HandshakeAsync(SslStreamTests._ec512Cert, protocol);

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        [BenchmarkCategory(Categories.NoAOT)]
        public Task HandshakeRSA2048CertAsync(SslProtocols protocol) => SslStreamTests.HandshakeAsync(SslStreamTests._rsa2048Cert, protocol);

        [Benchmark]
        [ArgumentsSource(nameof(TlsProtocols))]
        public Task HandshakeRSA4096CertAsync(SslProtocols protocol) => SslStreamTests.HandshakeAsync(SslStreamTests._rsa4096Cert, protocol);

        private static async Task<bool> GetTls13SupportAsync()
        {
            try
            {
                await SslStreamTests.HandshakeAsync(SslStreamTests._rsa2048Cert, SslProtocols.Tls13);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
