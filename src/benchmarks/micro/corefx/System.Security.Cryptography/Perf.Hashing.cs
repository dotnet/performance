// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Security.Cryptography.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Hashing
    {
        private readonly byte[] _data = ValuesGenerator.Array<byte>(100 * 1024 * 1024);

        private readonly SHA1 _sha1 = SHA1.Create();
        private readonly SHA256 _sha256 = SHA256.Create();
        private readonly SHA384 _sha384 = SHA384.Create();
        private readonly SHA512 _sha512 = SHA512.Create();

        [Benchmark]
        public byte[] Sha1() => _sha1.ComputeHash(_data);

        [Benchmark]
        public byte[] Sha256() => _sha256.ComputeHash(_data);

        [Benchmark]
        public byte[] Sha384() => _sha384.ComputeHash(_data);

        [Benchmark]
        public byte[] Sha512() => _sha512.ComputeHash(_data);
    }
}
