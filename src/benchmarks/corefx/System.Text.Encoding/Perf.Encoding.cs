// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.Text.Tests
{
    public class Perf_Encoding
    {
        [Params(16, 512)]
        public int size; // the field must be called length (starts with lowercase) to keep old benchmark id in BenchView, do NOT change it
        
        [Params("utf-8", "ascii")]
        public string encName; // the field must be called length (starts with lowercase) to keep old benchmark id in BenchView, do NOT change it

        private readonly PerfUtils _utils = new PerfUtils();
        
        private Encoding _enc;
        private string _toEncode;
        private byte[] _bytes;
        private char[] _chars;

        [GlobalSetup]
        public void SetupGetBytes()
        {
            _enc = Encoding.GetEncoding(encName);
            _toEncode = _utils.CreateString(size);
            _bytes = _enc.GetBytes(_toEncode);
            _chars = _toEncode.ToCharArray();
        }

        [Benchmark]
        public byte[] GetBytes() => _enc.GetBytes(_toEncode);

        [Benchmark]
        public string GetString() => _enc.GetString(_bytes);

        [Benchmark]
        public char[] GetChars() => _enc.GetChars(_bytes);

        [Benchmark]
        public Encoder GetEncoder() => _enc.GetEncoder();

        [Benchmark]
        public int GetByteCount() => _enc.GetByteCount(_chars);
    }
}
