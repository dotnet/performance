// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Collections.Generic;

namespace System.Text.Encodings.Web.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Encoders
    {
        public IEnumerable<object> GetEncoderArguments()
        {
            foreach (int size in new[] { 16, 512 })
            {
                yield return new EncoderArguments("no escaping required", size, JavaScriptEncoder.Default);
                yield return new EncoderArguments("&Hello+<World>!", size, JavaScriptEncoder.Default);
                yield return new EncoderArguments("no <escaping /> required", size, JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
                yield return new EncoderArguments("hello \"there\"", size, JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
                yield return new EncoderArguments("&lorem ipsum=dolor sit amet", size, UrlEncoder.Default);
                yield return new EncoderArguments("�2020", size, UrlEncoder.Default);
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetEncoderArguments))]
        [MemoryRandomization]
        public OperationStatus EncodeUtf8(EncoderArguments arguments) => arguments.EncodeUtf8();

        [Benchmark]
        [ArgumentsSource(nameof(GetEncoderArguments))]
        public OperationStatus EncodeUtf16(EncoderArguments arguments) => arguments.EncodeUtf16();

        public class EncoderArguments
        {
            private readonly string _sourceString;
            // pads the string with a pseudorandom number of non-escapable characters
            private readonly int _paddingSize;
            private readonly TextEncoder _encoder;
            private readonly string _sourceBufferUtf16;

            private readonly char[] _destinationBufferUtf16;
            private readonly byte[] _sourceBufferUtf8;
            private readonly byte[] _destinationBufferUtf8;

            public EncoderArguments(string sourceString, int paddingSize, TextEncoder encoder)
            {
                _sourceString = sourceString;
                _paddingSize = paddingSize;
                _encoder = encoder;

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

            public override string ToString() => $"{GetShortEncoderName()},{_sourceString},{_paddingSize}";

            // the name is displayed in the results in console, we want it as short as possible
            private string GetShortEncoderName()
            {
                if (_encoder.Equals(JavaScriptEncoder.Default))
                    return "JavaScript";
                if (_encoder.Equals(JavaScriptEncoder.UnsafeRelaxedJsonEscaping))
                    return "UnsafeRelaxed";
                if (_encoder.Equals(UrlEncoder.Default))
                    return "Url";
                throw new NotSupportedException("Unknown encoder.");
            }

            public OperationStatus EncodeUtf8() => _encoder.EncodeUtf8(_sourceBufferUtf8, _destinationBufferUtf8, out int _, out int _);

            public OperationStatus EncodeUtf16() => _encoder.Encode(_sourceBufferUtf16, _destinationBufferUtf16, out int _, out int _);
        }
    }
}
