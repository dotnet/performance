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
        private string _text;

        [ParamsSource(nameof(GetLineLengthRanges))]
        public Range LineLengthRange { get; set; }

        public static IEnumerable<Range> GetLineLengthRanges()
        {
            yield return new() { Min = 0, Max = 0 };
            yield return new() { Min = 1, Max = 1 };
            yield return new() { Min = 1, Max = 8 };
            yield return new() { Min = 9, Max = 32 };
            yield return new() { Min = 33, Max = 128 };
            yield return new() { Min = 129, Max = 1024 };
            yield return new() { Min = 0, Max = 1024 };
        }

        public class Range
        {
            public int Min { get; set; }
            public int Max { get; set; }

            public override string ToString() => $"[{Min,4}, {Max,4}]";
        }

        [GlobalSetup]
        public void GlobalSetup() => _text = GenerateLinesText(LineLengthRange, 16 * 1024);

        [Benchmark]
        public void ReadLine()
        {
            using StringReader reader = new (_text);
            while (reader.ReadLine() != null) ;
        }

        private static string GenerateLinesText(Range lineLengthRange, int textTargetLength)
        {
            var min = lineLengthRange.Min;
            var max = lineLengthRange.Max;

            var newLine = Environment.NewLine;

            var capacity = textTargetLength + max + newLine.Length;
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
