// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Security.Cryptography.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_CryptoConfig
    {
        [Benchmark]
        [Arguments("SHA512")]
        [Arguments("RSA")]
        [Arguments("X509Chain")]
#if NET5_0_OR_GREATER
#pragma warning disable SYSLIB0021 // Type or member is obsolete
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(SHA512Managed))]
#pragma warning restore SYSLIB0021 // Type or member is obsolete
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(X509Chain))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(RSACryptoServiceProvider))]
#endif
        public object CreateFromName(string name) => CryptoConfig.CreateFromName(name);
    }
}
