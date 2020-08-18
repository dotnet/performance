// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.IO;

namespace System.Text
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class Perf_Utf8String
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

        [ParamsAllValues] // BDN uses all values of given enum
        public InputFile Input { get; set; }

        private Utf8String _utf8;
        private Memory<char> _destination;

        [GlobalSetup]
        public void Setup()
        {
            string unicode = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "libraries", "System.Utf8String", $"{Input}.txt"));
            _utf8 = new Utf8String(unicode);
            _destination = new char[unicode.Length];
        }

        [Benchmark]
        public int ToChars() => new Utf8Span(_utf8).ToChars(_destination.Span);

        [Benchmark]
        public bool IsAscii() => new Utf8Span(_utf8).IsAscii();

        [Benchmark]
        public bool IsNormalized() => new Utf8Span(_utf8).IsNormalized();

        [Benchmark]
        public char[] ToCharArray() => new Utf8Span(_utf8).ToCharArray();
    }
}
