// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;

namespace System.Text.Json.Tests
{
    [GenericTypeArguments(typeof(LoginViewModel))]
    [GenericTypeArguments(typeof(Location))]
    [GenericTypeArguments(typeof(IndexViewModel))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel))]
    public class Json_ToString<T>
    {
        private readonly T _value;

        public Json_ToString() => _value = DataGenerator.Generate<T>();

        [BenchmarkCategory(Categories.CoreFX, Categories.JSON, Categories.JsonSerializer)]
        [Benchmark]
        public string SerializeJsonToString() => Serialization.JsonSerializer.ToString(_value);
    }
}
