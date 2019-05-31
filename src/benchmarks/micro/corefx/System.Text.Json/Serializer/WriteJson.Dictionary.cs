// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Text.Json.Serialization.Tests
{
    public class WriteDictionary
    {
        private Dictionary<string, string> _dict = new Dictionary<string, string>() { { "Hello", "World" }, { "Hello2", "World2" } };
        private static IDictionary<string, string> _iDict = new Dictionary<string, string>() { { "Hello", "World" }, { "Hello2", "World2" } };
        private IReadOnlyDictionary<string, string> _iReadOnlyDict = new Dictionary<string, string>() { { "Hello", "World" }, { "Hello2", "World2" } };

        private static ImmutableDictionary<string, string> _immutableDict;
        private static IImmutableDictionary<string, string> _iimmutableDict;
        private static ImmutableSortedDictionary<string, string> _immutableSortedDict;

        [GlobalSetup]
        public void Setup()
        {
            _immutableDict = ImmutableDictionary.CreateRange(_dict);
            _iimmutableDict = ImmutableDictionary.CreateRange(_dict);
            _immutableSortedDict = ImmutableSortedDictionary.CreateRange(_dict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeDict_ToUtf8Bytes()
        {
            return JsonSerializer.ToUtf8Bytes(_dict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeIDict_ToUtf8Bytes()
        {
            return JsonSerializer.ToUtf8Bytes(_iDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeIReadOnlyDict_ToUtf8Bytes()
        {
            return JsonSerializer.ToUtf8Bytes(_iReadOnlyDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializeDict_ToString()
        {
            return JsonSerializer.ToString(_dict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializeIDict_ToString()
        {
            return JsonSerializer.ToString(_iDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializeIReadOnlyDict_ToString()
        {
            return JsonSerializer.ToString(_iReadOnlyDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeImmutableDict_ToBytes()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToUtf8Bytes(_immutableDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeIImmutableDict_ToBytes()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToUtf8Bytes(_iimmutableDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public byte[] SerializeImmutableSortedDict_ToBytes()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToUtf8Bytes(_immutableSortedDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializeImmutableDict_ToString()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToString(_immutableDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializeIImmutableDict_ToString()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToString(_iimmutableDict);
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializeImmutableSortedDict_ToString()
        {
            return System.Text.Json.Serialization.JsonSerializer.ToString(_immutableSortedDict);
        }
    }
}
