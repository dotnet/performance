// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.IO;

namespace System.Text.Experimental
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class Perf_AsciiUtility
    {
        protected string ascii_11;
        protected Utf8String ascii_11_ustring;
        protected string nonascii_110;
        protected Utf8String nonascii_110_ustring;
        protected string nonascii_chinese;
        protected Utf8String nonascii_chinese_ustring;
        protected string nonascii_cyrillic;
        protected Utf8String nonascii_cyrillic_ustring;
        protected string nonascii_greek;
        protected Utf8String nonascii_greek_ustring;

        [GlobalSetup]
        public void Setup()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "libraries", "System.Utf8String.Experimental");
            ascii_11 = File.ReadAllText(Path.Combine(path, "11.txt"));
            ascii_11_ustring = new Utf8String(ascii_11);
            nonascii_110 = File.ReadAllText(Path.Combine(path, "11-0.txt"));
            nonascii_110_ustring = new Utf8String(nonascii_110);
            nonascii_chinese = File.ReadAllText(Path.Combine(path, "25249-0.txt"));
            nonascii_chinese_ustring = new Utf8String(nonascii_chinese);
            nonascii_cyrillic = File.ReadAllText(Path.Combine(path, "30774-0.txt"));
            nonascii_cyrillic_ustring = new Utf8String(nonascii_cyrillic);
            nonascii_greek = File.ReadAllText(Path.Combine(path, "39251-0.txt"));
            nonascii_greek_ustring = new Utf8String(nonascii_greek);
        }

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
