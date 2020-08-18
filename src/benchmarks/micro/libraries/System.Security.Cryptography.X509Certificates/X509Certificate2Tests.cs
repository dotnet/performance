// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class X509Certificate2Tests
    {
        private readonly X509Certificate2 _cert = GetMicrosoftComCert();

        private static X509Certificate2 GetMicrosoftComCert()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client.Connect("microsoft.com", 443);
                using (var ssl = new SslStream(new NetworkStream(client)))
                {
#if NETFRAMEWORK    // when using .None we were getting "The specified value is not valid in the 'SslProtocolType' enumeration" on Full Framework
                    ssl.AuthenticateAsClient("microsoft.com", null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false);
#else
                    ssl.AuthenticateAsClient("microsoft.com", null, SslProtocols.None, false);
#endif
                    return new X509Certificate2(ssl.RemoteCertificate);
                }
            }
        }

        [Benchmark]
        public string CertProp() => _cert.Thumbprint;
    }
}
