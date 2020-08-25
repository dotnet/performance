// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.IO;

namespace System.Text
{
    [BenchmarkCategory(Categories.Libraries, Categories.Runtime, Categories.NoWASM)]
    public class Perf_Utf8Encoding : Perf_TextBase
    {
        private string _unicode;
        private UTF8Encoding _utf8Encoding;

        [GlobalSetup]
        public void Setup()
        {
            _unicode = File.ReadAllText(Path.Combine(TextFilesRootPath, $"{Input}.txt"));
            _utf8Encoding = new UTF8Encoding();
        }

        [Benchmark]
        public int GetByteCount() => _utf8Encoding.GetByteCount(_unicode);
    }
}
