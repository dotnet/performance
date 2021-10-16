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
        public const int LineCount = 256_000;
        public const int ReaderCount = 10000;

        private string _text;
        private StringReader[] _readers;
        private int _readerIndex;
        private StringReader _reader;

        public StringReaderReadLineTests()
        {
            LineConfigs = new Config[]
            {
                new (){ LineLengthMin = 0, LineLengthMax = 0 },
                new (){ LineLengthMin = 0, LineLengthMax = 64 },
                new (){ LineLengthMin = 128, LineLengthMax = 512 },
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
                $"LineLength [{LineLengthMin,4},{LineLengthMax,4}]";
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            string text = GenerateLinesText(LineConfig);
            _text = text;
            File.WriteAllText(@$"D:\StringReaderReadLine-{LineConfig.LineLengthMin}-{LineConfig.LineLengthMax}.txt", _text);
            _readers = Enumerable.Range(0, ReaderCount + 1)
                .Select(i => new StringReader(_text)).ToArray();
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

        private static string GenerateLinesText(Config config)
        {
            var min = config.LineLengthMin;
            var max = config.LineLengthMax;
            var count = LineCount;

            var newLines = Math.Min(min, max) > 0 
                ? new[] { "\n", "\r", "\r\n" } 
                : new[] { "\r\n" };

            var capacity = (2 + max / 2 + min / 2) * count;
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
            var text = sb.ToString();
            return text;
        }
    }
}
