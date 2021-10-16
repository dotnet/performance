// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    [InvocationCount(InvocationCount)]
    public class StringReaderReadLineTests
    {
        public const int LineCount = 100_000;
        public const int ReaderCount = 100;
        public const int InvocationCount = LineCount * ReaderCount;

        private string _text;
        private StringReader[] _readers;
        private int _readerIndex;
        private StringReader _reader;

        public StringReaderReadLineTests()
        {
            LineConfigs = new[]
            {
                (LineLengthMin: 0, LineLengthMax: 64, LineCount),
                (LineLengthMin: 256, LineLengthMax: 512, LineCount),
            };
        }

        [ParamsSource(nameof(LineConfigs))]
        public (int LineLengthMin, int LineLengthMax, int LineCount) LineConfig { get; set; }

        public IEnumerable<(int LineLengthMin, int LineLengthMax, int LineCount)> LineConfigs { get; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var (min, max, count) = LineConfig;
            var capacity = (2 + max / 2 + min / 2) * count;
            var newLines = new[] { "\n", "\r", "\r\n" };
            var sb = new StringBuilder(capacity);
            var random = new Random(42);
            for (int l = 0; l < count; l++)
            {
                var charsCount = random.Next(min, max);
                for (int c = 0; c < charsCount; c++)
                {
                    var ch = (char)random.Next('0', 'z');
                    sb.Append(ch);
                }
                var newLine = newLines[random.Next(newLines.Length)];
                sb.Append(newLine);
            }
            _text = sb.ToString();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _readers = Enumerable.Range(0, ReaderCount + 1).Select(i =>new StringReader(_text)).ToArray();
            _readerIndex = 0;
            _reader = _readers[_readerIndex];
        }

        [Benchmark]
        public string ReadLine()
        {
            var line = _reader.ReadLine();
            if (line == null)
                _reader = _readers[++_readerIndex];
            return line;
        }
    }
}
