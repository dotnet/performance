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
    [BenchmarkCategory(Categories.CoreFX)]
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
                    ssl.AuthenticateAsClient("microsoft.com", null, SslProtocols.None, false);
                    return new X509Certificate2(ssl.RemoteCertificate);
                }
            }
        }

        [Benchmark]
        public string CertProp() => _cert.Thumbprint;
    }
}
