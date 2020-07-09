// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Experimental
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class Perf
    {
        public static IEnumerable<string> TranscodingTestData()
        {
            yield return "This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. This is a big string of words. ";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 256; i++)
            {
                sb.Append(i);
            }
            yield return sb.ToString();
        }

        [Benchmark]
        [ArgumentsSource(nameof(TranscodingTestData))]
        public void ToUtf16(string expected)
        {
            Utf8Span span = new Utf8Span(new Utf8String(expected));
            Memory<char> memory = new char[expected.Length];
            Span<char> destination = memory.Span;
            span.ToChars(destination);
        }
    }
}
