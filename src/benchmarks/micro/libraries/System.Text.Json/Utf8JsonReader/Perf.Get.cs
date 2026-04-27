// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Linq;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Get
    {
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

        [Benchmark]
        public bool GetBoolean()
        {
            bool result = false;
            var reader = new Utf8JsonReader(_jsonFalseBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result ^= reader.GetBoolean();
            }

            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public byte GetByte()
        {
            byte result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetByte();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public sbyte GetSByte()
        {
            sbyte result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetSByte();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public short GetInt16()
        {
            short result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetInt16();
            }
            return result;
        }

        [Benchmark]
        public int GetInt32()
        {
            int result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetInt32();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public long GetInt64()
        {
            long result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetInt64();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public ushort GetUInt16()
        {
            ushort result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetUInt16();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public uint GetUInt32()
        {
            uint result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetUInt32();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public ulong GetUInt64()
        {
            ulong result = 0;
            var reader = new Utf8JsonReader(_jsonIntegerNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetUInt64();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public float GetSingle()
        {
            float result = 0;
            var reader = new Utf8JsonReader(_jsonDecimalNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetSingle();
            }
            return result;
        }

        [Benchmark]
        public double GetDouble()
        {
            double result = 0;
            var reader = new Utf8JsonReader(_jsonDecimalNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetDouble();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public decimal GetDecimal()
        {
            decimal result = 0;
            var reader = new Utf8JsonReader(_jsonDecimalNumberBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result += reader.GetDecimal();
            }
            return result;
        }

        [Benchmark]
        public DateTime GetDateTime()
        {
            DateTime result = default;
            var reader = new Utf8JsonReader(_jsonDateTimeBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result = reader.GetDateTime();
            }
            return result;
        }

        [Benchmark]
        public DateTimeOffset GetDateTimeOffset()
        {
            DateTimeOffset result = default;
            var reader = new Utf8JsonReader(_jsonDateTimeOffsetBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result = reader.GetDateTimeOffset();
            }
            return result;
        }

        [Benchmark]
        [MemoryRandomization]
        public Guid GetGuid()
        {
            Guid result = default;
            var reader = new Utf8JsonReader(_jsonGuidBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result = reader.GetGuid();
            }
            return result;
        }

        [Benchmark]
        public string GetString()
        {
            string result = default;
            var reader = new Utf8JsonReader(_jsonStringBytes);
            reader.Read();

            for (int i = 0; i < 100; i++)
            {
                result = reader.GetString();
            }
            return result;
        }
    }
}
