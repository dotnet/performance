// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ustring = System.Utf8String;

namespace System.Text.Experimental
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class Perf
    {
        public static string ascii_11;
        public static string nonascii_110;
        public static string nonascii_chinese;
        public static string nonascii_cyrillic;
        public static string nonascii_greek;
        public static IEnumerable<string> NonAsciiData()
        {
            yield return nonascii_110;
            yield return nonascii_chinese;
            yield return nonascii_cyrillic;
            yield return nonascii_greek;
        }

        [GlobalSetup]
        public void InitializeData()
        {
            ascii_11 = File.ReadAllText("../../../../../src/benchmarks/micro/libraries/System.Utf8String.Experimental/11.txt");
            nonascii_110 = File.ReadAllText("../../../../../src/benchmarks/micro/libraries/System.Utf8String.Experimental/11-0.txt");
            nonascii_chinese = File.ReadAllText("../../../../../src/benchmarks/micro/libraries/System.Utf8String.Experimental/25249-0.txt");
            nonascii_cyrillic = File.ReadAllText("../../../../../src/benchmarks/micro/libraries/System.Utf8String.Experimental/30774-0.txt");
            nonascii_greek = File.ReadAllText("../../../../../src/benchmarks/micro/libraries/System.Utf8String.Experimental/39251-0.txt");
        }

        public static IEnumerable<string> AsciiData()
        {
            yield return ascii_11;
        }

        [Benchmark]
        [ArgumentsSource(nameof(NonAsciiData))]
        public void ToUtf16(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            Memory<char> memory = new char[expected.Length];
            Span<char> destination = memory.Span;
            span.ToChars(destination);
        }

        [Benchmark]
        [ArgumentsSource(nameof(NonAsciiData))]
        public void IsAscii(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            span.IsAscii();
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsciiData))]
        public void IsAscii_GetIndexOfFirstNonAsciiByte(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            span.IsAscii();
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsciiData))]
        public void IsNormalized_GetIndexOfFirstNonAsciiChar(string expected)
        {
            expected.IsNormalized();
            expected.IsNormalized();
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsciiData))]
        public void IsNormalized_WidenAsciiToUtf16(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            char[] returned = span.ToCharArray();
            char[] second = span.ToCharArray();
        }
    }
}
