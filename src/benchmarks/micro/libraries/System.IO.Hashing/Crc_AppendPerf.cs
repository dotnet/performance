// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.IO.Hashing.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public abstract class Crc_AppendPerf<T>
        where T : NonCryptographicHashAlgorithm, new()
    {
        protected T Crc;
        protected byte[] Buffer;

        public abstract int BufferSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Buffer = ValuesGenerator.Array<byte>(BufferSize);
            Crc = new T();
        }

        [Benchmark]
        public void Append()
        {
            Crc.Append(Buffer);
        }
    }
}
