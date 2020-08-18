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
        // the benchmark uses text files from Project Gutenberg
        public enum InputFile
        {
            EnglishAllAscii, // English, all-ASCII, should stay entirely within fast paths
            EnglishMostlyAscii, // English, mostly ASCII with some rare non-ASCII chars, exercises that the occasional non-ASCII char doesn't kill our fast paths
            Chinese, // Chinese, exercises 3-byte scalar processing paths typical of East Asian languages
            Cyrillic, // Cyrillic, exercises a combination of ASCII and 2-byte scalar processing paths
            Greek, // Greek, similar to the Cyrillic case but with a different distribution of ASCII and non-ASCII chars
        }

        private string _englishAllAsciiUnicode;
        private Utf8String _englishAllAsciiUtf8;
        private string _englishMostlyAsciiUnicode;
        private Utf8String _englishMostlyAsciiUtf8;
        private string _chineseUnicode;
        private Utf8String _chineseUtf8;
        private string _cyrillicUnicode;
        private Utf8String _cyrillicUtf8;
        private string _greekUnicode;
        private Utf8String _greekUtf8;

        [GlobalSetup]
        public void Setup()
        {
            _englishAllAsciiUnicode = LoadFile(InputFile.EnglishAllAscii);
            _englishAllAsciiUtf8 = new Utf8String(_englishAllAsciiUnicode);
            _englishMostlyAsciiUnicode = LoadFile(InputFile.EnglishMostlyAscii);
            _englishMostlyAsciiUtf8 = new Utf8String(_englishMostlyAsciiUnicode);
            _chineseUnicode = LoadFile(InputFile.Chinese);
            _chineseUtf8 = new Utf8String(_chineseUnicode);
            _cyrillicUnicode = LoadFile(InputFile.Cyrillic);
            _cyrillicUtf8 = new Utf8String(_cyrillicUnicode);
            _greekUnicode = LoadFile(InputFile.Greek);
            _greekUtf8 = new Utf8String(_greekUnicode);
        }

        private string LoadFile(InputFile inputFile) => File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "libraries", "System.Utf8String.Experimental", $"{inputFile}.txt"));


        [Benchmark]
        public int ToUtf16_nonascii_110()
        {
            string str = _englishMostlyAsciiUnicode;
            Utf8Span span = new Utf8Span(_englishMostlyAsciiUtf8);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public int ToUtf16_nonascii_chinese()
        {
            string str = _chineseUnicode;
            Utf8Span span = new Utf8Span(_chineseUtf8);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public int ToUtf16_nonascii_cyrillic()
        {
            string str = _cyrillicUnicode;
            Utf8Span span = new Utf8Span(_cyrillicUtf8);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public int ToUtf16_nonascii_greek()
        {
            string str = _greekUnicode;
            Utf8Span span = new Utf8Span(_greekUtf8);
            Memory<char> memory = new char[str.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        public bool IsAscii_GetIndexOfFirstNonAsciiByte()
        {
            Utf8Span ascii_11_span = new Utf8Span(_englishAllAsciiUtf8);
            return ascii_11_span.IsAscii();
        }

        [Benchmark]
        public bool IsNormalized_GetIndexOfFirstNonAsciiChar()
        {
            bool b1 = _englishAllAsciiUnicode.IsNormalized();
            bool b2 = _englishAllAsciiUnicode.IsNormalized();
            return b1 & b2;
        }

        [Benchmark]
        public void IsNormalized_WidenAsciiToUtf16()
        {
            Utf8Span ascii_11_span = new Utf8Span(_englishAllAsciiUtf8);
            char[] returned = ascii_11_span.ToCharArray();
            char[] second = ascii_11_span.ToCharArray();
            if (returned[0] != second[0])
            {
                Console.WriteLine("Just a line to consume returned and second");
            }
        }
    }
}
