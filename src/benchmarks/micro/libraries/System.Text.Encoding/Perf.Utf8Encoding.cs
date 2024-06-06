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
        private string _unicode;
        private byte[] _bytes;
        private UTF8Encoding _utf8Encoding;

        [GlobalSetup]
        public void Setup()
        {
            _unicode = File.ReadAllText(Path.Combine(TextFilesRootPath, $"{Input}.txt"));
            _utf8Encoding = new UTF8Encoding();
            _bytes = _utf8Encoding.GetBytes(_unicode);
        }

        [Benchmark]
        [MemoryRandomization]
        public int GetByteCount() => _utf8Encoding.GetByteCount(_unicode);

        [Benchmark]
        public byte[] GetBytes() => _utf8Encoding.GetBytes(_unicode);

        [Benchmark]
        public string GetString() => _utf8Encoding.GetString(_bytes);
    }
}
