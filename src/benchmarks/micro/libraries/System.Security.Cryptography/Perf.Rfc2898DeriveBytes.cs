// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Security.Cryptography.Tests
{
#if !NET10_0_OR_GREATER
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_Rfc2898DeriveBytes
    {
        private static readonly Rfc2898DeriveBytes s_db = new Rfc2898DeriveBytes("verysafepassword", 32, 10_000, HashAlgorithmName.SHA256);

        [Benchmark]
        public byte[] DeriveBytes() => s_db.GetBytes(32);
    }
#endif
}
