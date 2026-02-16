// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class BinaryWriterExtendedTests
    {
        private string _asciiInput;
        private char[] _asciiInputAsChars;
        private string _nonAsciiInput;
        private char[] _nonAsciiInputAsChars;
        private BinaryWriter _bw;

        [Params(32, 8_000, 2_000_000)]
        public int StringLengthInChars;

        [GlobalSetup]
        public void Setup()
        {
            _bw = new BinaryWriter(new NullWriteStream());

            _asciiInput = new string('x', StringLengthInChars);
            _asciiInputAsChars = _asciiInput.ToCharArray();
            _nonAsciiInput = new string('\u00E0', StringLengthInChars);
            _nonAsciiInputAsChars = _nonAsciiInput.ToCharArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteAsciiCharArray()
        {
            _bw.Write(_asciiInputAsChars);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteAsciiString()
        {
            _bw.Write(_asciiInput);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteNonAsciiCharArray()
        {
            _bw.Write(_nonAsciiInputAsChars);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteNonAsciiString()
        {
            _bw.Write(_nonAsciiInput);
        }
    }

    /// <summary>
    /// Benchmarks for BinaryWriter with a non-UTF-8 encoding, exercising the
    /// _useFastUtf8 = false code path.
    /// </summary>
    [BenchmarkCategory(Categories.Libraries)]
    public class BinaryWriterUnicodeEncodingTests
    {
        private string _asciiInput;
        private char[] _asciiInputAsChars;
        private string _nonAsciiInput;
        private char[] _nonAsciiInputAsChars;
        private BinaryWriter _bw;

        [Params(32, 8_000, 2_000_000)]
        public int StringLengthInChars;

        [GlobalSetup]
        public void Setup()
        {
            _bw = new BinaryWriter(new NullWriteStream(), Encoding.Unicode);

            _asciiInput = new string('x', StringLengthInChars);
            _asciiInputAsChars = _asciiInput.ToCharArray();
            _nonAsciiInput = new string('\u00E0', StringLengthInChars);
            _nonAsciiInputAsChars = _nonAsciiInput.ToCharArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteAsciiCharArray()
        {
            _bw.Write(_asciiInputAsChars);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteAsciiString()
        {
            _bw.Write(_asciiInput);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteNonAsciiCharArray()
        {
            _bw.Write(_nonAsciiInputAsChars);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteNonAsciiString()
        {
            _bw.Write(_nonAsciiInput);
        }
    }
}
