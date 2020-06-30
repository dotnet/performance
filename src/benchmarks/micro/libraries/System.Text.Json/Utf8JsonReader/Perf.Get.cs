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
        private byte[] _jsonSByteBytes;
        private byte[] _jsonInt16Bytes;
        private byte[] _jsonInt32Bytes;
        private byte[] _jsonInt64Bytes;

        private byte[] _jsonSingleBytes;
        private byte[] _jsonDoubleBytes;
        private byte[] _jsonDecimalBytes;

        private byte[] _jsonStringBytes;
        private byte[] _jsonGuidBytes;
        private byte[] _jsonDateTimeBytes;
        private byte[] _jsonDateTimeOffsetBytes;

        [Params(true, false)]
        public bool UseLargeValue;

        [GlobalSetup]
        public void Setup()
        {
            if (UseLargeValue)
            {
                _jsonSByteBytes = Encoding.UTF8.GetBytes(sbyte.MaxValue.ToString());
                _jsonInt16Bytes = Encoding.UTF8.GetBytes(short.MaxValue.ToString());
                _jsonInt32Bytes = Encoding.UTF8.GetBytes(int.MaxValue.ToString());
                _jsonInt64Bytes = Encoding.UTF8.GetBytes(long.MaxValue.ToString());

                _jsonSingleBytes = Encoding.UTF8.GetBytes(float.MaxValue.ToString("F"));
                _jsonDoubleBytes = Encoding.UTF8.GetBytes(double.MaxValue.ToString("F"));
                _jsonDecimalBytes = Encoding.UTF8.GetBytes(decimal.MaxValue.ToString("F"));
                _jsonStringBytes = Encoding.UTF8.GetBytes($"\"{new string('a', 1024 * 2)}\"");
            }
            else
            {
                _jsonSByteBytes = Encoding.UTF8.GetBytes(default(sbyte).ToString());
                _jsonInt16Bytes = Encoding.UTF8.GetBytes(default(short).ToString());
                _jsonInt32Bytes = Encoding.UTF8.GetBytes(default(int).ToString());
                _jsonInt64Bytes = Encoding.UTF8.GetBytes(default(long).ToString());

                _jsonSingleBytes = Encoding.UTF8.GetBytes(default(float).ToString());
                _jsonDoubleBytes = Encoding.UTF8.GetBytes(default(double).ToString());
                _jsonDecimalBytes = Encoding.UTF8.GetBytes(default(decimal).ToString());
                _jsonStringBytes = Encoding.UTF8.GetBytes("\"The quick brown fox jumps over the lazy dog.\"");
            }

            _jsonGuidBytes = Encoding.UTF8.GetBytes($"\"{Guid.Empty}\"");
            _jsonDateTimeBytes = Encoding.UTF8.GetBytes($"\"{DateTime.MaxValue:O}\"");
            _jsonDateTimeOffsetBytes = Encoding.UTF8.GetBytes($"\"{DateTimeOffset.MaxValue:O}\"");
        }

        [Benchmark]
        public byte GetByte()
        {
            var reader = new Utf8JsonReader(_jsonSByteBytes);
            reader.Read();
            return reader.GetByte();
        }

        [Benchmark]
        public sbyte GetSByte()
        {
            var reader = new Utf8JsonReader(_jsonSByteBytes);
            reader.Read();
            return reader.GetSByte();
        }

        [Benchmark]
        public short GetInt16()
        {
            var reader = new Utf8JsonReader(_jsonInt16Bytes);
            reader.Read();
            return reader.GetInt16();
        }

        [Benchmark]
        public int GetInt32()
        {
            var reader = new Utf8JsonReader(_jsonInt32Bytes);
            reader.Read();
            return reader.GetInt32();
        }

        [Benchmark]
        public long GetInt64()
        {
            var reader = new Utf8JsonReader(_jsonInt64Bytes);
            reader.Read();
            return reader.GetInt64();
        }

        [Benchmark]
        public ushort GetUInt16()
        {
            var reader = new Utf8JsonReader(_jsonInt16Bytes);
            reader.Read();
            return reader.GetUInt16();
        }

        [Benchmark]
        public uint GetUInt32()
        {
            var reader = new Utf8JsonReader(_jsonInt32Bytes);
            reader.Read();
            return reader.GetUInt32();
        }

        [Benchmark]
        public ulong GetUInt64()
        {
            var reader = new Utf8JsonReader(_jsonInt64Bytes);
            reader.Read();
            return reader.GetUInt64();
        }

        [Benchmark]
        public float GetSingle()
        {
            var reader = new Utf8JsonReader(_jsonSingleBytes);
            reader.Read();
            return reader.GetSingle();
        }

        [Benchmark]
        public double GetDouble()
        {
            var reader = new Utf8JsonReader(_jsonDoubleBytes);
            reader.Read();
            return reader.GetDouble();
        }

        [Benchmark]
        public decimal GetDecimal()
        {
            var reader = new Utf8JsonReader(_jsonDecimalBytes);
            reader.Read();
            return reader.GetDecimal();
        }

        [Benchmark]
        public DateTime GetDateTime()
        {
            var reader = new Utf8JsonReader(_jsonDateTimeBytes);
            reader.Read();
            return reader.GetDateTime();
        }

        [Benchmark]
        public DateTimeOffset GetDateTimeOffset()
        {
            var reader = new Utf8JsonReader(_jsonDateTimeOffsetBytes);
            reader.Read();
            return reader.GetDateTimeOffset();
        }

        [Benchmark]
        public Guid GetGuid()
        {
            var reader = new Utf8JsonReader(_jsonGuidBytes);
            reader.Read();
            return reader.GetGuid();
        }

        [Benchmark]
        public string GetString()
        {
            var reader = new Utf8JsonReader(_jsonStringBytes);
            reader.Read();
            return reader.GetString();
        }
    }
}
