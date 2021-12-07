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
    public class StreamReaderReadLineTests : TextReaderReadLineTests
    {
        private MemoryStream _stream;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _text = GenerateLinesText(LineLengthRange, 16 * 1024);
            _stream = new(Encoding.UTF8.GetBytes(_text));
        }

        [Benchmark]
        public void ReadLine()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            using StreamReader reader = new (_stream, null, true, 1024, leaveOpen: true);
            while (reader.ReadLine() != null) ;
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        public async Task ReadLineAsync()
        {
            _stream.Seek(0, SeekOrigin.Begin);
            using StreamReader reader = new(_stream, null, true, 1024, leaveOpen: true);
            while (await reader.ReadLineAsync() != null) ;
        }
    }
}
