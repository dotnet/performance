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
        private byte[] _bytes;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _text = GenerateLinesText(LineLengthRange, 16 * 1024);
            _bytes = Encoding.UTF8.GetBytes(_text);
        }

        [Benchmark]
        [MemoryRandomization]
        public void ReadLine()
        {
            using (StreamReader reader = new StreamReader(new MemoryStream(_bytes)))
            {
                while (reader.ReadLine() != null) ;
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [MemoryRandomization]
        public async Task ReadLineAsync()
        {
            using (StreamReader reader = new StreamReader(new MemoryStream(_bytes)))
            {
                while (await reader.ReadLineAsync() != null) ;
            }
        }
    }
}
