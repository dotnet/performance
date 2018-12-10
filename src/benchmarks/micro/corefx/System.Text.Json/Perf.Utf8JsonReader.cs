// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class Perf_Utf8JsonReader
    {
        [ParamsSource(nameof(DataGenerator))]
        public JsonNumberSet NumberSet { get; set; }

        public IEnumerable<JsonNumberSet> DataGenerator()
        {
            yield return new JsonNumberSet("positive numbers", "1234", "12345678901", "1234.5678", "1234.5678", "1234.5678");
            
            yield return new JsonNumberSet("positive numbers e2", "1234e2", "12345678901e2", "1234.5678e2", "1234.5678e2", "1234.5678e2");
            yield return new JsonNumberSet("positive numbers e+2", "1234e+2", "12345678901e+2", "1234.5678e+2", "1234.5678e+2", "1234.5678e+2");
            yield return new JsonNumberSet("positive numbers e-2", "1234e-2", "12345678901e-2", "1234.5678e-2", "1234.5678e-2", "1234.5678e-2");
            
            yield return new JsonNumberSet("positive numbers E2", "1234E2", "12345678901E2", "1234.5678E2", "1234.5678E2", "1234.5678E2");
            yield return new JsonNumberSet("positive numbers E+2", "1234E+2", "12345678901E+2", "1234.5678E+2", "1234.5678E+2", "1234.5678E+2");
            yield return new JsonNumberSet("positive numbers E-2", "1234E-2", "12345678901E-2", "1234.5678E-2", "1234.5678E-2", "1234.5678E-2");
            
            yield return new JsonNumberSet("negative numbers", "-1234", "-12345678901", "-1234.5678", "-1234.5678", "-1234.5678");
            
            yield return new JsonNumberSet("negative numbers e2", "-1234e2", "-12345678901e2", "-1234.5678e2", "-1234.5678e2", "-1234.5678e2");
            yield return new JsonNumberSet("negative numbers e+2", "-1234e+2", "-12345678901e+2", "-1234.5678e+2", "-1234.5678e+2", "-1234.5678e+2");
            yield return new JsonNumberSet("negative numbers e-2", "-1234e-2", "-12345678901e-2", "-1234.5678e-2", "-1234.5678e-2", "-1234.5678e-2");
            
            yield return new JsonNumberSet("negative numbers E2", "-1234E2", "-12345678901E2", "-1234.5678E2", "-1234.5678E2", "-1234.5678E2");
            yield return new JsonNumberSet("negative numbers E+2", "-1234E+2", "-12345678901E+2", "-1234.5678E+2", "-1234.5678E+2", "-1234.5678E+2");
            yield return new JsonNumberSet("negative numbers E-2", "-1234E-2", "-12345678901E-2", "-1234.5678E-2", "-1234.5678E-2", "-1234.5678E-2");
        }

        [Benchmark]
        public int GetInt32Value() => GetInt32(NumberSet.int32Json);

        [Benchmark]
        public long GetInt64Value() => GetInt64(NumberSet.int64Json);

        [Benchmark]
        public float GetSingleValue() => GetSingleValue(NumberSet.floatJson);

        [Benchmark]
        public double GetDoubleValue() => GetDoubleValue(NumberSet.doubleJson);

        [Benchmark]
        public decimal GetDecimalValue() => GetDecimalValue(NumberSet.decimalJson);

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
