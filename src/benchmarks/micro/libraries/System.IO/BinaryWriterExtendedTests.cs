// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class BinaryWriterExtendedTests
    {
        private string _input;
        private char[] _inputAsChars;
        private BinaryWriter _bw;

        [Params(32, 8_000, 2_000_000)]
        public int StringLengthInChars;

        [GlobalSetup]
        public void Setup()
        {
            _bw = new BinaryWriter(new NullWriteStream());

            _input = new string('x', StringLengthInChars);
            _inputAsChars = _input.ToCharArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteAsciiCharArray()
        {
            _bw.Write(_inputAsChars);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteAsciiString()
        {
            _bw.Write(_input);
        }
    }
}
