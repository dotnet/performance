// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography.X509Certificates;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public partial class X509ChainTests
    {
        private static readonly X509Certificate2 _cert = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate();
        private static readonly X509Certificate2 _rsa4096Cert = System.Net.Test.Common.Configuration.Certificates.GetRSA4096Certificate();

        private bool BuildChain(X509Certificate2 certificate)
        {
            X509Chain chain = new X509Chain();
            return chain.Build(certificate);
        }

        [Benchmark]
        public bool BuildX509ChainSelfSigned() => BuildChain(_rsa4096Cert);

        [Benchmark]
        public bool BuildX509ChainContoso() => BuildChain(_cert);
    }
}
