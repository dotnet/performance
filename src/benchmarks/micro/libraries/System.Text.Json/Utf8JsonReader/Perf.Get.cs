// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Get
    {
        private const int OperationsPerBenchmark = 100;

        private static readonly byte[] _jsonFalseBytes = GetJsonBytes("false");
        private static readonly byte[] _jsonIntegerNumberBytes = GetJsonBytes(123);
        private static readonly byte[] _jsonDecimalNumberBytes = GetJsonBytes(123.456f);
        private static readonly byte[] _jsonStringBytes = GetJsonBytes("\"The quick brown fox jumps over the lazy dog.\"");
        private static readonly byte[] _jsonGuidBytes = GetJsonBytes($"\"{Guid.Empty}\"");
        private static readonly byte[] _jsonDateTimeBytes = GetJsonBytes($"\"{DateTime.MaxValue:O}\"");
        private static readonly byte[] _jsonDateTimeOffsetBytes = GetJsonBytes($"\"{DateTimeOffset.MaxValue:O}\"");

        private static byte[] GetJsonBytes<T>(T elem)
        {
            return Encoding.UTF8.GetBytes(elem.ToString());
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetBoolean()
        {
            var reader = new Utf8JsonReader(_jsonFalseBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetBoolean();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetByte()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetByte();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetSByte()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetSByte();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetInt16()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetInt16();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetInt32()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();
            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetInt32();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetInt64()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();
            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetInt64();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetUInt16()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();
            for (int i = 0; i < 100; i++)
            {
                reader.GetUInt16();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetUInt32()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();
            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetUInt32();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetUInt64()
        {
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();
            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetUInt64();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetSingle()
        {
            var reader = new Utf8JsonReader(_jsonDecimalNumberBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetSingle();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetDouble()
        {
            var reader = new Utf8JsonReader(_jsonDecimalNumberBytes);
            reader.Read();
            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetDouble();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetDecimal()
        {
            var reader = new Utf8JsonReader(_jsonDecimalNumberBytes);
            reader.Read();
            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetDecimal();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetDateTime()
        {
            var reader = new Utf8JsonReader(_jsonDateTimeBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetDateTime();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetDateTimeOffset()
        {
            var reader = new Utf8JsonReader(_jsonDateTimeOffsetBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetDateTimeOffset();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetGuid()
        {
            var reader = new Utf8JsonReader(_jsonGuidBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetGuid();
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerBenchmark)]
        public void GetString()
        {
            var reader = new Utf8JsonReader(_jsonStringBytes);
            reader.Read();

            for (int i = 0; i < OperationsPerBenchmark; i++)
            {
                reader.GetString();
            }
        }
    }
}
