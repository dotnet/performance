// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Guid
    {
        const string guidStr = "a8a110d5-fc49-43c5-bf46-802db8f843ff";
        
        private readonly Guid _guid;
        private readonly Guid _same;
        private readonly byte[] _buffer;

        public Perf_Guid()
        {
            _guid = new Guid(guidStr);
            _same = new Guid(guidStr);
            _buffer = _guid.ToByteArray();
        }

        [Benchmark]
        public Guid NewGuid() => Guid.NewGuid();

        [Benchmark]
        public Guid ctor_str() => new Guid(guidStr);

        [Benchmark]
        public Guid ctor_bytes() => new Guid(_buffer);

        [Benchmark]
        public bool EqualsSame() => _guid.Equals(_same);

        [Benchmark]
        public Guid Parse() => Guid.Parse(guidStr);

        [Benchmark]
        public Guid ParseExactD() => Guid.ParseExact(guidStr, "D");

        [Benchmark]
        public string GuidToString() => _guid.ToString();

#if !NETFRAMEWORK
        [Benchmark]
        public bool TryWriteBytes() => _guid.TryWriteBytes(_buffer);
#endif
    }
}