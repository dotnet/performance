// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Strings
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private string[] _stringArrayValues;
        private byte[][] _stringArrayValuesUtf8;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        public enum Escape
        {
            AllEscaped,
            OneEscaped,
            NoneEscaped
        }

        [Params(Escape.AllEscaped, Escape.OneEscaped, Escape.NoneEscaped)]
        public Escape Escaped;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            _stringArrayValues = new string[DataSize];
            _stringArrayValuesUtf8 = new byte[DataSize][];

            var random = new Random(42);

            for (int i = 0; i < DataSize; i++)
            {
                _stringArrayValues[i] = GetString(random, 5, 100, Escaped);
                _stringArrayValuesUtf8[i] = Encoding.UTF8.GetBytes(_stringArrayValues[i]);
            }
        }

        private static string GetString(Random random, int minLength, int maxLength, Escape escape)
        {
            int length = random.Next(minLength, maxLength);
            var array = new char[length];

            if (escape != Escape.AllEscaped)
            {
                for (int i = 0; i < length; i++)
                {
                    array[i] = (char)random.Next(97, 123);
                }

                if (escape == Escape.OneEscaped)
                {
                    if (random.NextDouble() > 0.5)
                    {
                        array[random.Next(0, length)] = '"';
                    }
                }
            }
            else
            {
                array.AsSpan().Fill('"');
            }

            return new string(array);
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteStringsUtf8()
        {
            _arrayBufferWriter.Clear();
            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {

                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteStringValue(_stringArrayValuesUtf8[i]);
                }
                json.WriteEndArray();
                json.Flush();
            }
        }

        [Benchmark]
        [MemoryRandomization]
        public void WriteStringsUtf16()
        {
            _arrayBufferWriter.Clear();
            using (var json = new Utf8JsonWriter(_arrayBufferWriter, new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation }))
            {

                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteStringValue(_stringArrayValues[i]);
                }
                json.WriteEndArray();
                json.Flush();
            }
        }
    }
}
