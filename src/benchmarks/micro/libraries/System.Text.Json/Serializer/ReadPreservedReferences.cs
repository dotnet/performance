﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using Newtonsoft.Json;

namespace System.Text.Json.Serialization.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(SimpleListOfInt))]
    [GenericTypeArguments(typeof(SimpleStructWithProperties))]
    public class ReadPreservedReferences<T>
    {
        [Params(false, true)]
        public bool IsDataPreserved;

        private string _serialized;
        private JsonSerializerOptions _options;
        private JsonSerializerSettings _settings;

        [GlobalSetup]
        public void Setup()
        {
            _options = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };

            _settings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All };

            T value = DataGenerator.Generate<T>();

            if (IsDataPreserved)
            {
                _serialized = JsonConvert.SerializeObject(value, _settings);
            }
            else
            {
                // Use payload that does not contain metadata in order to see what is the penalty of having ReferenceHandler.Preserve set.
                _serialized = JsonConvert.SerializeObject(value);
            }
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        public T DeserializePreserved() => JsonSerializer.Deserialize<T>(_serialized, _options);

        [BenchmarkCategory(Categories.ThirdParty, Categories.JSON)]
        [Benchmark(Baseline = true)]
        public T NewtonsoftDeserializePreserved() => JsonConvert.DeserializeObject<T>(_serialized, _settings);
    }    
}
*/