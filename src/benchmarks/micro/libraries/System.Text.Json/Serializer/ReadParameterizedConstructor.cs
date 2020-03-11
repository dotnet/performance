// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.IO;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    [GenericTypeArguments(typeof(SimpleStructWithProperties), typeof(SimpleStructWithProperties_Immutable), typeof(SimpleStructWithProperties_1Arg))]
    [GenericTypeArguments(typeof(LoginViewModel), typeof(Parameterized_LoginViewModel_Immutable), typeof(Parameterized_LoginViewModel_2Args))]
    [GenericTypeArguments(typeof(Location), typeof(Parameterized_Location_Immutable), typeof(Parameterized_Location_5Args))]
    [GenericTypeArguments(typeof(IndexViewModel), typeof(Parameterized_IndexViewModel_Immutable), typeof(Parameterized_IndexViewModel_2Args))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel), typeof(Parameterized_MyEventsListerViewModel_Immutable), typeof(Parameterized_MyEventsListerViewModel_2Args))]
    [GenericTypeArguments(typeof(Parameterless_Point), typeof(Parameterized_Point_Immutable), typeof(Parameterized_Point_1Arg))]
    [GenericTypeArguments(typeof(Parameterless_ClassWithPrimitives), typeof(Parameterized_ClassWithPrimitives_Immutable), typeof(Parameterized_ClassWithPrimitives_4Args))]
    public class ReadParameterizedConstructor<TTypeWithParameterlessCtor, TTypeWithParameterizedCtor1, TTypeWithParameterizedCtorType2>
    {
        private string _serialized;

        [GlobalSetup]
        public void Setup()
        {
            TTypeWithParameterlessCtor value0 = DataGenerator.Generate<TTypeWithParameterlessCtor>();
            _serialized = JsonSerializer.Serialize(value0);
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark(Baseline = true)]
        public TTypeWithParameterlessCtor Deserialize_Parameterless() => JsonSerializer.Deserialize<TTypeWithParameterlessCtor>(_serialized);

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        // Immutable object; all JSON maps to constructor arguments
        public TTypeWithParameterizedCtor1 Deserialize_Parameterized_Immutable() => JsonSerializer.Deserialize<TTypeWithParameterizedCtor1>(_serialized);

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        // Mutable object; half of the JSON maps to constructor arguments
        public TTypeWithParameterizedCtorType2 Deserialize_Parameterized_Mutable() => JsonSerializer.Deserialize<TTypeWithParameterizedCtorType2>(_serialized);
    }

    [GenericTypeArguments(typeof(SimpleStructWithProperties), typeof(SimpleStructWithProperties_Immutable), typeof(SimpleStructWithProperties_1Arg))]
    [GenericTypeArguments(typeof(LoginViewModel), typeof(Parameterized_LoginViewModel_Immutable), typeof(Parameterized_LoginViewModel_2Args))]
    [GenericTypeArguments(typeof(Location), typeof(Parameterized_Location_Immutable), typeof(Parameterized_Location_5Args))]
    [GenericTypeArguments(typeof(IndexViewModel), typeof(Parameterized_IndexViewModel_Immutable), typeof(Parameterized_IndexViewModel_2Args))]
    [GenericTypeArguments(typeof(MyEventsListerViewModel), typeof(Parameterized_MyEventsListerViewModel_Immutable), typeof(Parameterized_MyEventsListerViewModel_2Args))]
    [GenericTypeArguments(typeof(Parameterless_Point), typeof(Parameterized_Point_Immutable), typeof(Parameterized_Point_1Arg))]
    [GenericTypeArguments(typeof(Parameterless_ClassWithPrimitives), typeof(Parameterized_ClassWithPrimitives_Immutable), typeof(Parameterized_ClassWithPrimitives_4Args))]
    public class ReadParameterizedConstructorAsync<TTypeWithParameterlessCtor, TTypeWithParameterizedCtor1, TTypeWithParameterizedCtor2>
    {
        private MemoryStream _memoryStream;

        [GlobalSetup]
        public async Task Setup()
        {
            TTypeWithParameterlessCtor value = DataGenerator.Generate<TTypeWithParameterlessCtor>();

            _memoryStream = new MemoryStream(capacity: short.MaxValue);
            await JsonSerializer.SerializeAsync(_memoryStream, value);
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark(Baseline = true)]
        public async Task<TTypeWithParameterlessCtor> DeserializeAsync_Parameterless()
        {
            _memoryStream.Position = 0;
            var value = await JsonSerializer.DeserializeAsync<TTypeWithParameterlessCtor>(_memoryStream);
            return value;
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        // Immutable object; all JSON maps to constructor arguments
        public async Task<TTypeWithParameterizedCtor1> DeserializeAsync_Parameterized_Immutable()
        {
            _memoryStream.Position = 0;
            var value = await JsonSerializer.DeserializeAsync<TTypeWithParameterizedCtor1>(_memoryStream);
            return value;
        }

        [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
        [Benchmark]
        // Mutable object; half of the JSON maps to constructor arguments
        public async Task<TTypeWithParameterizedCtor2> DeserializeAsync_Parameterized_Mutable()
        {
            _memoryStream.Position = 0;
            var value = await JsonSerializer.DeserializeAsync<TTypeWithParameterizedCtor2>(_memoryStream);
            return value;
        }
    }
}
