// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Guid
    {
        const string guidStr = "a8a110d5-fc49-43c5-bf46-802db8f843ff";
        private readonly Guid _guid = new Guid(guidStr);
        private readonly byte[] _buffer = new byte[16];

        [Benchmark]
        public Guid NewGuid() => Guid.NewGuid();

        [Benchmark]
        public Guid ctor_str() => new Guid(guidStr);

        [Benchmark]
        public Guid Parse() => Guid.Parse(guidStr);

        [Benchmark]
        public Guid ParseExactD() => Guid.ParseExact(guidStr, "D");

        [Benchmark]
        public string GuidToString() => _guid.ToString();

#if !NETFRAMEWORK
        [Benchmark]
        public Guid ToFromBytes()
        {
            _guid.TryWriteBytes(_buffer);
            return new Guid(_buffer);
        }
#endif
    }
}