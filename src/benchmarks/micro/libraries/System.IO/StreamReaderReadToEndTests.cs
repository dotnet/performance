// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class StreamReaderReadToEndTests : TextReaderReadLineTests
    {
        private StreamReader _reader;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _text = GenerateLinesText(LineLengthRange, 48 * 1024 * 1024);
            _reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(_text)));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _reader?.Dispose();
        }

        [Benchmark]
        [MemoryRandomization]
        public string ReadToEnd()
        {
            _reader.BaseStream.Position = 0;
            _reader.DiscardBufferedData();
            return _reader.ReadToEnd();
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [MemoryRandomization]
        public Task<string> ReadToEndAsync()
        {
            _reader.BaseStream.Position = 0;
            _reader.DiscardBufferedData();
            return _reader.ReadToEndAsync();
        }
    }
}