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
        // Benchmark is based on measuring individual `ReadLine` calls
        // of lines of different lengths within a range defined as
        // benchmark parameters.
        // To fulfill BDNs requirement for an iteration time of 250ms,
        // this means a lot of invocations, more than a single string in
        // a `StringReader` can accommodate.
        // Therefore, the benchmark generates of string with a certain
        // target length and then a number of readers for the same
        // string, since `StringReader` does not support being "reset".
        // The trade off is we need to switch reader during the `ReadLine`
        // call when current reader ends, which adds a bit of overhead. 
        // This is deemed negligible and amortized constant.
        private const int StringReaderTargetLength = 16 * 1024 * 1024;
        private const int ReaderCount = 1000;

        private string _text;
        private StringReader[] _readers;
        private int _readerIndex;
        private StringReader _reader;

        public StringReaderReadLineTests()
        {
            LineLengthRanges = new Range[]
            {
                new (){ Min =   0, Max =    0 },
                new (){ Min =   1, Max =    8 },
                new (){ Min =   9, Max =   32 },
                new (){ Min =  33, Max =  128 },
                new (){ Min = 129, Max = 1024 },
            };
        }

        [ParamsSource(nameof(LineLengthRanges))]
        public Range LineLengthRange { get; set; }

        public IEnumerable<Range> LineLengthRanges { get; }

        public class Range
        {
            public int Min { get; set; }
            public int Max { get; set; }

            public override string ToString() => $"[{Min,4},{Max,4}]";
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _text = GenerateLinesText(LineLengthRange, StringReaderTargetLength);
            _readers = Enumerable.Range(0, ReaderCount)
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

        private static string GenerateLinesText(Range lineLengthRange, int textTargetLength)
        {
            var min = lineLengthRange.Min;
            var max = lineLengthRange.Max;

            // Just to make things interesting 
            // new line characters are randomized.
            // Note if lines are empty not \r and \n
            // might be "combined".
            var newLines = new[] { "\n", "\r", "\r\n" };

            var capacity = textTargetLength + max + 2;
            var sb = new StringBuilder(capacity);

            var random = new Random(42);
            int lineCount = 0;
            while(sb.Length < textTargetLength)
            {
                var charsCount = random.Next(min, max);
                for (int c = 0; c < charsCount; c++)
                {
                    var ch = (char)random.Next('0', 'z');
                    sb.Append(ch);
                }
                var newLine = newLines[random.Next(newLines.Length)];
                sb.Append(newLine);
                ++lineCount;
            }
            var text = sb.ToString();

            Console.WriteLine($"// Generated lines {lineCount} and " +
                $"text length {text.Length} out of capacity {capacity}");

            return text;
        }
    }
}
