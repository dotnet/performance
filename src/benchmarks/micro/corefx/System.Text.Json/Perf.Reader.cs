// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Tests
{
    public class JsonDataSet
    {
        private readonly string description;
        public readonly byte[] int32Json;
        public readonly byte[] int64Json;
        public readonly byte[] floatJson;
        public readonly byte[] doubleJson;
        public readonly byte[] decimalJson;

        public JsonDataSet(string description, string int32Json, string int64Json, string floatJson, string doubleJson, string decimalJson)
        {
            this.description = description;
            this.int32Json = Encoding.UTF8.GetBytes(int32Json);
            this.int64Json = Encoding.UTF8.GetBytes(int64Json);
            this.floatJson = Encoding.UTF8.GetBytes(floatJson);
            this.doubleJson = Encoding.UTF8.GetBytes(doubleJson);
            this.decimalJson = Encoding.UTF8.GetBytes(decimalJson);
        }

        public override string ToString() => description;
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Reader
    {
        [ParamsSource(nameof(DataGenerator))]
        public JsonDataSet DataSet { get; set; }

        public IEnumerable<JsonDataSet> DataGenerator()
        {
            yield return new JsonDataSet(
                "positive numbers",
                @"{""id"": 1234}",
                @"{""id"": 12345678901}",
                @"{""id"": 1234.5678}",
                @"{""id"": 1234.5678}",
                @"{""id"": 1234.5678}");

            yield return new JsonDataSet(
                "positive numbers e2",
                @"{""id"": 1234e2}",
                @"{""id"": 12345678901e2}",
                @"{""id"": 1234.5678e2}",
                @"{""id"": 1234.5678e2}",
                @"{""id"": 1234.5678e2}");

            yield return new JsonDataSet(
                "positive numbers e+2",
                @"{""id"": 1234e+2}",
                @"{""id"": 12345678901e+2}",
                @"{""id"": 1234.5678e+2}",
                @"{""id"": 1234.5678e+2}",
                @"{""id"": 1234.5678e+2}");

            yield return new JsonDataSet(
                "positive numbers e-2",
                @"{""id"": 1234e-2}",
                @"{""id"": 12345678901e-2}",
                @"{""id"": 1234.5678e-2}",
                @"{""id"": 1234.5678e-2}",
                @"{""id"": 1234.5678e-2}");

            yield return new JsonDataSet(
                "positive numbers E2",
                @"{""id"": 1234E2}",
                @"{""id"": 12345678901E2}",
                @"{""id"": 1234.5678E2}",
                @"{""id"": 1234.5678E2}",
                @"{""id"": 1234.5678E2}");

            yield return new JsonDataSet(
                "positive numbers E+2",
                @"{""id"": 1234E+2}",
                @"{""id"": 12345678901E+2}",
                @"{""id"": 1234.5678E+2}",
                @"{""id"": 1234.5678E+2}",
                @"{""id"": 1234.5678E+2}");

            yield return new JsonDataSet(
                "positive numbers E-2",
                @"{""id"": 1234E-2}",
                @"{""id"": 12345678901E-2}",
                @"{""id"": 1234.5678E-2}",
                @"{""id"": 1234.5678E-2}",
                @"{""id"": 1234.5678E-2}");

            yield return new JsonDataSet(
                "negative numbers",
                @"{""id"": -1234}",
                @"{""id"": -12345678901}",
                @"{""id"": -1234.5678}",
                @"{""id"": -1234.5678}",
                @"{""id"": -1234.5678}");

            yield return new JsonDataSet(
                "negative numbers e2",
                @"{""id"": -1234e2}",
                @"{""id"": -12345678901e2}",
                @"{""id"": -1234.5678e2}",
                @"{""id"": -1234.5678e2}",
                @"{""id"": -1234.5678e2}");

            yield return new JsonDataSet(
                "negative numbers e+2",
                @"{""id"": -1234e+2}",
                @"{""id"": -12345678901e+2}",
                @"{""id"": -1234.5678e+2}",
                @"{""id"": -1234.5678e+2}",
                @"{""id"": -1234.5678e+2}");

            yield return new JsonDataSet(
                "negative numbers e-2",
                @"{""id"": -1234e-2}",
                @"{""id"": -12345678901e-2}",
                @"{""id"": -1234.5678e-2}",
                @"{""id"": -1234.5678e-2}",
                @"{""id"": -1234.5678e-2}");

            yield return new JsonDataSet(
                "negative numbers E2",
                @"{""id"": -1234E2}",
                @"{""id"": -12345678901E2}",
                @"{""id"": -1234.5678E2}",
                @"{""id"": -1234.5678E2}",
                @"{""id"": -1234.5678E2}");

            yield return new JsonDataSet(
                "negative numbers E+2",
                @"{""id"": -1234E+2}",
                @"{""id"": -12345678901E+2}",
                @"{""id"": -1234.5678E+2}",
                @"{""id"": -1234.5678E+2}",
                @"{""id"": -1234.5678E+2}");

            yield return new JsonDataSet(
                "negative numbers E-2",
                @"{""id"": -1234E-2}",
                @"{""id"": -12345678901E-2}",
                @"{""id"": -1234.5678E-2}",
                @"{""id"": -1234.5678E-2}",
                @"{""id"": -1234.5678E-2}");
        }

        [Benchmark]
        public int GetInt32Value() => GetInt32(DataSet.int32Json);

        [Benchmark]
        public long GetInt64Value() => GetInt64(DataSet.int64Json);

        [Benchmark]
        public float GetSingleValue() => GetSingleValue(DataSet.floatJson);

        [Benchmark]
        public double GetDoubleValue() => GetDoubleValue(DataSet.doubleJson);

        [Benchmark]
        public decimal GetDecimalValue() => GetDecimalValue(DataSet.decimalJson);

        private int GetInt32(ReadOnlySpan<byte> value)
        {
            var json = new Utf8JsonReader(value, isFinalBlock: true, state: default);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                if (tokenType == JsonTokenType.Number)
                {
                    if (json.TryGetInt32Value(out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        private long GetInt64(ReadOnlySpan<byte> value)
        {
            var json = new Utf8JsonReader(value, isFinalBlock: true, state: default);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                if (tokenType == JsonTokenType.Number)
                {
                    if (json.TryGetInt64Value(out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        private float GetSingleValue(ReadOnlySpan<byte> value)
        {
            var json = new Utf8JsonReader(value, isFinalBlock: true, state: default);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                if (tokenType == JsonTokenType.Number)
                {
                    if (json.TryGetSingleValue(out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        private double GetDoubleValue(ReadOnlySpan<byte> value)
        {
            var json = new Utf8JsonReader(value, isFinalBlock: true, state: default);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                if (tokenType == JsonTokenType.Number)
                {
                    if (json.TryGetDoubleValue(out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        private decimal GetDecimalValue(ReadOnlySpan<byte> value)
        {
            var json = new Utf8JsonReader(value, isFinalBlock: true, state: default);

            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;

                if (tokenType == JsonTokenType.Number)
                {
                    if (json.TryGetDecimalValue(out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }
    }
}
