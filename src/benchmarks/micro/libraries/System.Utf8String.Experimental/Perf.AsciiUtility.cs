// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Experimental
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class Perf_AsciiUtility : Perf_Utf8String_Base
    {
        [Benchmark]
        public int ToUtf16_nonascii_110()
        {
            string str = nonascii_110;
            Utf8Span span = new Utf8Span(nonascii_110_ustring);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public int ToUtf16_nonascii_chinese()
        {
            string str = nonascii_chinese;
            Utf8Span span = new Utf8Span(nonascii_chinese_ustring);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public int ToUtf16_nonascii_cyrillic()
        {
            string str = nonascii_cyrillic;
            Utf8Span span = new Utf8Span(nonascii_cyrillic_ustring);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public int ToUtf16_nonascii_greek()
        {
            string str = nonascii_greek;
            Utf8Span span = new Utf8Span(nonascii_greek_ustring);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public bool IsAscii_GetIndexOfFirstNonAsciiByte()
        {
            Utf8Span ascii_11_span = new Utf8Span(ascii_11_ustring);
            return ascii_11_span.IsAscii();
        }

        [Benchmark]
        public bool IsNormalized_GetIndexOfFirstNonAsciiChar()
        {
            bool b1 = ascii_11.IsNormalized();
            bool b2 = ascii_11.IsNormalized();
            return b1 & b2;
        }

        [Benchmark]
        public void IsNormalized_WidenAsciiToUtf16()
        {
            Utf8Span ascii_11_span = new Utf8Span(ascii_11_ustring);
            char[] returned = ascii_11_span.ToCharArray();
            char[] second = ascii_11_span.ToCharArray();
            if (returned[0] != second[0])
            {
                Console.WriteLine("Just a line to consume returned and second");
            }
        }
    }
}
