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
        private static readonly byte[] _jsonNumberBytes = GetJsonBytes(default(int));
        private static readonly byte[] _jsonStringBytes = GetJsonBytes("\"The quick brown fox jumps over the lazy dog.\"");
        private static readonly byte[] _jsonGuidBytes = GetJsonBytes($"\"{Guid.Empty}\"");
        private static readonly byte[] _jsonDateTimeBytes = GetJsonBytes($"\"{DateTime.MaxValue:O}\"");
        private static readonly byte[] _jsonDateTimeOffsetBytes = GetJsonBytes($"\"{DateTimeOffset.MaxValue:O}\"");

        private static byte[] GetJsonBytes<T>(T elem)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(string.Join(',', Enumerable.Repeat(elem, 100)));
            sb.Append(']');

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        [Benchmark]
        public void ReadToEndNumber()
        {
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number) ;
        }

        [Benchmark]
        public void ReadToEndString()
        {
            var reader = new Utf8JsonReader(_jsonStringBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.String) ;
        }

        [Benchmark]
        public void ReadToEndGuid()
        {
            var reader = new Utf8JsonReader(_jsonGuidBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.String) ;
        }

        [Benchmark]
        public void ReadToEndDateTime()
        {
            var reader = new Utf8JsonReader(_jsonDateTimeBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.String) ;
        }

        [Benchmark]
        public void ReadToEndDateTimeOffset()
        {
            var reader = new Utf8JsonReader(_jsonDateTimeOffsetBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.String) ;
        }

        [Benchmark]
        public byte GetByte()
        {
            byte result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetByte();
            }
            return result;
        }

        [Benchmark]
        public sbyte GetSByte()
        {
            sbyte result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetSByte();
            }
            return result;
        }

        [Benchmark]
        public short GetInt16()
        {
            short result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetInt16();
            }
            return result;
        }

        [Benchmark]
        public int GetInt32()
        {
            int result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetInt32();
            }
            return result;
        }

        [Benchmark]
        public long GetInt64()
        {
            long result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetInt64();
            }
            return result;
        }

        [Benchmark]
        public ushort GetUInt16()
        {
            ushort result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetUInt16();
            }
            return result;
        }

        [Benchmark]
        public uint GetUInt32()
        {
            uint result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetUInt32();
            }
            return result;
        }

        [Benchmark]
        public ulong GetUInt64()
        {
            ulong result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetUInt64();
            }
            return result;
        }

        [Benchmark]
        public float GetSingle()
        {
            float result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetSingle();
            }
            return result;
        }

        [Benchmark]
        public double GetDouble()
        {
            double result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
            {
                result += reader.GetDouble();
            }
            return result;
        }

        [Benchmark]
        public decimal GetDecimal()
        {
            decimal result = 0;
            var reader = new Utf8JsonReader(_jsonNumberBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.Number)
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
            while (reader.Read() && reader.TokenType == JsonTokenType.String)
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
            while (reader.Read() && reader.TokenType == JsonTokenType.String)
            {
                result = reader.GetDateTimeOffset();
            }
            return result;
        }

        [Benchmark]
        public Guid GetGuid()
        {
            Guid result = default;
            var reader = new Utf8JsonReader(_jsonGuidBytes);
            reader.Read();
            while (reader.Read() && reader.TokenType == JsonTokenType.String)
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
            while (reader.Read() && reader.TokenType == JsonTokenType.String)
            {
                result = reader.GetString();
            }
            return result;
        }
    }
}
