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
            string path = System.IO.Directory.GetParent(Environment.CurrentDirectory).ToString();
            int cc = 0;
            while (!path.EndsWith("performance"))
            {
                path = System.IO.Directory.GetParent(path).ToString();
                cc++;
                if (cc > 20)
                {
                    // An infinite loop?
                    throw new Exception("Unable to determine path to test files");
                }
            }
            path = Path.Combine(path, "src/benchmarks/micro/libraries/System.Utf8String.Experimental/");
            ascii_11 = File.ReadAllText(path + "11.txt");
            nonascii_110 = File.ReadAllText(path + "11-0.txt");
            nonascii_chinese = File.ReadAllText(path + "25249-0.txt");
            nonascii_cyrillic = File.ReadAllText(path + "30774-0.txt");
            nonascii_greek = File.ReadAllText(path + "39251-0.txt");
        }

        public static IEnumerable<string> AsciiData()
        {
            yield return ascii_11;
        }

        [Benchmark]
        [ArgumentsSource(nameof(NonAsciiData))]
        public int ToUtf16(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            Memory<char> memory = new char[expected.Length];
            Span<char> destination = memory.Span;
            return span.ToChars(destination);
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsciiData))]
        public bool IsAscii_GetIndexOfFirstNonAsciiByte(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            return span.IsAscii();
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsciiData))]
        public bool IsNormalized_GetIndexOfFirstNonAsciiChar(string expected)
        {
            bool b1 = expected.IsNormalized();
            bool b2 = expected.IsNormalized();
            return b1 & b2;
        }

        [Benchmark]
        [ArgumentsSource(nameof(AsciiData))]
        public void IsNormalized_WidenAsciiToUtf16(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            char[] returned = span.ToCharArray();
            char[] second = span.ToCharArray();
            if (returned[0] != second[0])
            {
                Console.WriteLine("Just a line to consume returned and second");
            }
        }
    }
}
