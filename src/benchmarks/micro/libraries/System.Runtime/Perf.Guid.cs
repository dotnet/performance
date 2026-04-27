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
        const string guid2Str = "86A96B5D-F9B2-4CB5-B531-FC7021FA99D7";
        
        private readonly Guid _guid;
        private readonly Guid _same;
        private readonly Guid _guid2;
        private readonly byte[] _buffer;

        public Perf_Guid()
        {
            _guid = new Guid(guidStr);
            _same = new Guid(guidStr);
            _guid2 = new Guid(guid2Str);
            _buffer = _guid.ToByteArray();
        }

        [Benchmark]
        public Guid NewGuid() => Guid.NewGuid();

        [Benchmark]
        [MemoryRandomization]
        public Guid ctor_str() => new Guid(guidStr);

        [Benchmark]
        public Guid ctor_bytes() => new Guid(_buffer);

        [Benchmark]
        public bool EqualsSame() => _guid.Equals(_same);

        [Benchmark]
        public bool EqualsNotSame() => _guid.Equals(_guid2);

        [Benchmark]
        public bool EqualsOperator() => _guid == _same;

        [Benchmark]
        public bool NotEqualsOperator() => _guid != _same;

        [Benchmark]
        [MemoryRandomization]
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
