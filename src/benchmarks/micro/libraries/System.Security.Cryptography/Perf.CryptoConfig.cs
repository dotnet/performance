// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Security.Cryptography.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_CryptoConfig
    {
        [Benchmark]
        [Arguments("SHA512")]
        [Arguments("RSA")]
        [Arguments("X509Chain")]
        public object CreateFromName(string name) => CryptoConfig.CreateFromName(name);
    }
}
