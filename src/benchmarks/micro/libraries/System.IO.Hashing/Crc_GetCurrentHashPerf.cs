// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.IO.Hashing.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public abstract class Crc_GetCurrentHashPerf<T>
        where T : NonCryptographicHashAlgorithm, new()
    {
        protected T Crc;
        protected byte[] HashBuffer;

        [GlobalSetup]
        public void Setup()
        {
            Crc = new T();
            Crc.Append(ValuesGenerator.Array<byte>(128));
            HashBuffer = new byte[Crc.HashLengthInBytes];
        }

        [Benchmark]
        public int GetCurrentHash()
        {
            return Crc.GetCurrentHash(HashBuffer);
        }
    }
}
