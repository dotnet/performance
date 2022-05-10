// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class StringReaderReadToEndTests : TextReaderReadLineTests
    {
        [GlobalSetup]
        public void GlobalSetup() => _text = GenerateLinesText(LineLengthRange, 48 * 1024 * 1024);

        [Benchmark]
        public string ReadToEnd()
        {
            // StringReaders cannot be reset, so we are forced to include the constructor in the benchmark
            using StringReader reader = new StringReader(_text);
            return reader.ReadToEnd();
        }

        [BenchmarkCategory(Categories.NoWASM)]
        [Benchmark]
        public Task<string> ReadToEndAsync()
        {
            using StringReader reader = new StringReader(_text);
            return reader.ReadToEndAsync();
        }
    }
}