// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Text.Encodings.Web;

namespace System.Text.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_UnsafeRelaxedJavaScriptEncoder
    {
        [Params("no <escaping /> required", "hello \"there\"")]
        public string sourceString;

        // pads the string with a pseudorandom number of non-escapable characters
        [Params(16, 512)]
        public int paddingSize;

        private JavaScriptEncoder _encoder;
        private string _sourceBufferUtf16;
        private char[] _destinationBufferUtf16;

        private byte[] _sourceBufferUtf8;
        private byte[] _destinationBufferUtf8;

        [GlobalSetup]
        public void SetupGetBytes()
        {
            _encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            _sourceBufferUtf16 = BuildSourceString();
            _destinationBufferUtf16 = new char[paddingSize + 10 * sourceString.Length];

            _sourceBufferUtf8 = Encoding.UTF8.GetBytes(_sourceBufferUtf16);
            _destinationBufferUtf8 = new byte[paddingSize + 10 * sourceString.Length];
            
            string BuildSourceString()
            {
                var sb = new StringBuilder();
                // pad the string with `paddingSize` non-escapable ascii characters
                var random = new Random(42);
                for (int i = 0; i < paddingSize; i++)
                {
                    sb.Append((char)random.Next('a', 'z' + 1));
                }
                sb.Append(sourceString);
                return sb.ToString();
            }
        }

        [Benchmark]
        public void EncodeUtf8() => _encoder.EncodeUtf8(_sourceBufferUtf8, _destinationBufferUtf8, out int _, out int _);

        [Benchmark]
        public void EncodeUtf16() => _encoder.Encode(_sourceBufferUtf16, _destinationBufferUtf16, out int _, out int _);
    }
}
