// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;

namespace System.Text.Json
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Strings
    {
        private const int DataSize = 100_000;

        private ArrayBufferWriter<byte> _arrayBufferWriter;
        private PooledBufferWriter<byte> _pooledBufferWriter;

        private string[] _stringArrayValues;
        private byte[][] _stringArrayValuesUtf8;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [Params(true, false)]
        public bool Pool;

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
            _pooledBufferWriter = new PooledBufferWriter<byte>();

            _stringArrayValues = new string[DataSize];
            _stringArrayValuesUtf8 = new byte[DataSize][];

            for (int i = 0; i < DataSize; i++)
            {
                _stringArrayValues[i] = GetString(5, 100, Escaped);
                _stringArrayValuesUtf8[i] = Encoding.UTF8.GetBytes(_stringArrayValues[i]);
            }
        }

        private static string GetString(int minLength, int maxLength, Escape escape)
        {
            var random = new Random(42);
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
        public void WriteStringsUtf8()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartArray();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStringValue(_stringArrayValuesUtf8[i]);
            }
            json.WriteEndArray();
            json.Flush(isFinalBlock: true);
        }

        [Benchmark]
        public void WriteStringsUtf16()
        {
            IBufferWriter<byte> output = GetOutput();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(output, state);

            json.WriteStartArray();
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteStringValue(_stringArrayValues[i]);
            }
            json.WriteEndArray();
            json.Flush(isFinalBlock: true);
        }

        private IBufferWriter<byte> GetOutput()
        {
            if (Pool)
            {
                _pooledBufferWriter.Clear();
                return _pooledBufferWriter;
            }
            else
            {
                _arrayBufferWriter.Clear();
                return _arrayBufferWriter;
            }
        }
    }

    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Newtonsoft_Strings
    {
        private const int DataSize = 100_000;

        private MemoryStream _memoryStream;

        private string[] _stringArrayValues;

        private TextWriter _writer;

        [Params(Formatting.Indented, Formatting.None)]
        public Formatting Formatting;

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
            _memoryStream = new MemoryStream();
            _writer = new StreamWriter(_memoryStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

            _stringArrayValues = new string[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _stringArrayValues[i] = GetString(5, 100, Escaped);
            }
        }

        private static string GetString(int minLength, int maxLength, Escape escape)
        {
            var random = new Random(42);
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
        public void WriteStrings()
        {
            _memoryStream.Seek(0, SeekOrigin.Begin);
            TextWriter output = _writer;
            using (var json = new JsonTextWriter(output))
            {
                json.Formatting = Formatting;

                json.WriteStartArray();
                for (int i = 0; i < DataSize; i++)
                {
                    json.WriteValue(_stringArrayValues[i]);
                }
                json.WriteEndArray();
                json.Flush();
            }
        }
    }
}
