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
    public class StringReaderReadLineTests
    {
        public const int LineCount = 100_000;
        public const int ReaderCount = 10000;
        public const int InvocationCount = LineCount * ReaderCount;

        private string _text;
        private StringReader[] _readers;
        private int _readerIndex;
        private StringReader _reader;

        public StringReaderReadLineTests()
        {
            LineConfigs = new Config[]
            {
                new Config(){LineLengthMin = 0, LineLengthMax = 0 },
                new Config(){LineLengthMin = 0, LineLengthMax = 64 },
                new Config(){LineLengthMin = 128, LineLengthMax = 512 },
            };
        }

        [ParamsSource(nameof(LineConfigs))]
        public Config LineConfig { get; set; }

        public IEnumerable<Config> LineConfigs { get; }

        public class Config
        {
            public int LineLengthMin { get; set; }
            public int LineLengthMax { get; set; }

            public override string ToString() =>
                $"{nameof(LineLengthMin)}={LineLengthMin} " +
                $"{nameof(LineLengthMax)}={LineLengthMin}";
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var min = LineConfig.LineLengthMin;
            var max = LineConfig.LineLengthMax;
            var count = LineCount;
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
            //File.WriteAllText(@"D:\StringReaderReadLine.txt", _text);
            _readers = Enumerable.Range(0, ReaderCount + 1).Select(i => new StringReader(_text)).ToArray();
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
