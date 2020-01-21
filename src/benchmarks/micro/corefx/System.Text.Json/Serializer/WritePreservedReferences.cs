// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using Newtonsoft.Json;
using System.Reflection;

namespace System.Text.Json.Serialization.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    [GenericTypeArguments(typeof(SimpleListOfInt))]
    [GenericTypeArguments(typeof(SimpleStructWithProperties))]
    public class WritePreservedReferences<T>
    {
        private T _value;
        private JsonSerializerOptions _options;
        private JsonSerializerSettings _settings;

        [GlobalSetup]
        public void Setup()
        {
            _value = DataGenerator.Generate<T>();

            _options = new JsonSerializerOptions();
            _options.ReferenceHandling = ReferenceHandling.Preserve;

            _settings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All };
        }

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public string SerializePreserved() => JsonSerializer.Serialize(_value, _options);

        [BenchmarkCategory(Categories.ThirdParty, Categories.JSON)]
        [Benchmark(Baseline = true)]
        public string NewtonsoftSerializePreserved() => JsonConvert.SerializeObject(_value, _settings);
    }
}
