// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class StringReaderReadLineTests : TextReaderReadLineTests
    {
        [GlobalSetup]
        public void GlobalSetup() => _text = GenerateLinesText(LineLengthRange, 16 * 1024);

        [Benchmark]
        public void ReadLine()
        {
            using (StringReader reader = new StringReader(_text))
            {
                while (reader.ReadLine() != null) ;
            }
        }

        [Benchmark]
        [BenchmarkCategory(Categories.NoWASM)]
        [MemoryRandomization]
        public async Task ReadLineAsync()
        {
            using (StringReader reader = new StringReader(_text))
            {
                while (await reader.ReadLineAsync() != null) ;
            }
        }
    }
}
