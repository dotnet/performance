using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;

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
            _options = new JsonSerializerOptions();

            // Once the API is merged, replace this with _options.ReferenceHandling = ReferenceHandling.Preserve;
            PropertyInfo referenceHandlingOption = _options.GetType().GetProperty("ReferenceHandling");
            Type refHandlingType = referenceHandlingOption.PropertyType;
            PropertyInfo preserve = refHandlingType.GetProperty("Preserve", BindingFlags.Public | BindingFlags.Static);
            referenceHandlingOption.SetValue(_options, preserve.GetValue(null, null));

            _settings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All };

            T value = DataGenerator.Generate<T>();

            if (IsDataPreserved)
            {
                _serialized = JsonConvert.SerializeObject(value, _settings);
            }
            else
            {
                // Use payload that does not contain metadata in order to see what is the penalty of having ReferenceHandling.Preserve set.
                _serialized = JsonConvert.SerializeObject(value);
            }
        }


        [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
        [Benchmark]
        public T DeserializePreserved() => JsonSerializer.Deserialize<T>(_serialized, _options);

        [BenchmarkCategory(Categories.ThirdParty, Categories.JSON)]
        [Benchmark]
        public T NewtonsoftDeserializePreserved() => JsonConvert.DeserializeObject<T>(_serialized, _settings);
    }    
}
