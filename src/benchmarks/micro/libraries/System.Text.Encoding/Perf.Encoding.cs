// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Encoding
    {
        [Params(16, 512)]
        public int size; 
        
        [Params("utf-8", "ascii")]
        public string encName; 

        private Encoding _enc;
        private string _toEncode;
        private byte[] _bytes;
        private char[] _chars;

        [GlobalSetup]
        public void SetupGetBytes()
        {
            _enc = Encoding.GetEncoding(encName);
            _toEncode = PerfUtils.CreateString(size);
            _bytes = _enc.GetBytes(_toEncode);
            _chars = _toEncode.ToCharArray();
        }

        [Benchmark]
        [MemoryRandomization]
        public byte[] GetBytes() => _enc.GetBytes(_toEncode);

        [Benchmark]
        [MemoryRandomization]
        public string GetString() => _enc.GetString(_bytes);

        [Benchmark]
        [MemoryRandomization]
        public char[] GetChars() => _enc.GetChars(_bytes);

        [Benchmark]
        [MemoryRandomization]
        public Encoder GetEncoder() => _enc.GetEncoder();

        [Benchmark]
        [MemoryRandomization]
        public int GetByteCount() => _enc.GetByteCount(_chars);
    }
}
