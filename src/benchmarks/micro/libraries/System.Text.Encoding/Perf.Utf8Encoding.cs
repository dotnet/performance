// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.IO;

namespace System.Text
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime)]
    public class Perf_Utf8Encoding : Perf_TextBase
    {
        private string _string;
        private byte[] _bytes;
        private char[] _chars;

        [GlobalSetup]
        public void Setup()
        {
            _string = File.ReadAllText(Path.Combine(TextFilesRootPath, $"{Input}.txt"));
            _bytes = Encoding.UTF8.GetBytes(_string);
            _chars = _string.ToCharArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public int GetByteCount() => Encoding.UTF8.GetByteCount(_string);

        [Benchmark]
        [MemoryRandomization]
        public int GetCharCount() => Encoding.UTF8.GetCharCount(_bytes);

        [Benchmark]
        public int GetBytesFromChars() => Encoding.UTF8.GetBytes(_chars.AsSpan(), _bytes);

        [Benchmark]
        public int GetCharsFromBytes() => Encoding.UTF8.GetChars(_bytes.AsSpan(), _chars);

        [Benchmark]
        public string GetStringFromBytes() => Encoding.UTF8.GetString(_bytes);

        [Benchmark]
        public byte[] GetBytesFromString() => Encoding.UTF8.GetBytes(_string);
    }
}
