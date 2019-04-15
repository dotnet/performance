// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Basic
    {
        private static readonly byte[] ExtraArrayUtf8 = Encoding.UTF8.GetBytes("ExtraArray");
        private static readonly byte[] FirstUtf8 = Encoding.UTF8.GetBytes("first");
        private static readonly byte[] LastUtf8 = Encoding.UTF8.GetBytes("last");
        private static readonly byte[] AgeUtf8 = Encoding.UTF8.GetBytes("age");
        private static readonly byte[] PhoneNumbersUtf8 = Encoding.UTF8.GetBytes("phoneNumbers");
        private static readonly byte[] AddressUtf8 = Encoding.UTF8.GetBytes("address");
        private static readonly byte[] StreetUtf8 = Encoding.UTF8.GetBytes("street");
        private static readonly byte[] CityUtf8 = Encoding.UTF8.GetBytes("city");
        private static readonly byte[] ZipUtf8 = Encoding.UTF8.GetBytes("zip");

        private ArrayBufferWriter<byte> _arrayBufferWriter;

        private string[] _propertyNames;
        private byte[][] _propertyNamesUtf8;
        private int[] _numberArrayValues;
        private string[] _stringArrayValues;

        [Params(true, false)]
        public bool Formatted;

        [Params(true, false)]
        public bool SkipValidation;

        [Params(10, 100_000)]
        public int DataSize;

        [GlobalSetup]
        public void Setup()
        {
            _arrayBufferWriter = new ArrayBufferWriter<byte>();

            var random = new Random(42);

            _propertyNames = new string[DataSize];
            _propertyNamesUtf8 = new byte[DataSize][];
            _numberArrayValues = new int[DataSize];
            _stringArrayValues = new string[DataSize];

            for (int i = 0; i < DataSize; i++)
            {
                _propertyNames[i] = "abcde" + i.ToString();
                _propertyNamesUtf8[i] = Encoding.UTF8.GetBytes(_propertyNames[i]);
                int value = random.Next(-10000, 10000);
                _numberArrayValues[i] = value;
                _stringArrayValues[i] = value.ToString();
            }
        }

        [Benchmark]
        public void WriteBasicUtf8()
        {
            _arrayBufferWriter.Clear();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(_arrayBufferWriter, state);

            json.WriteStartObject();
            json.WriteNumber(AgeUtf8, 42);
            json.WriteString(FirstUtf8, "John");
            json.WriteString(LastUtf8, "Smith");
            json.WriteStartArray(PhoneNumbersUtf8);
            json.WriteStringValue("425-000-1212");
            json.WriteStringValue("425-000-1213");
            json.WriteEndArray();
            json.WriteStartObject(AddressUtf8);
            json.WriteString(StreetUtf8, "1 Microsoft Way");
            json.WriteString(CityUtf8, "Redmond");
            json.WriteNumber(ZipUtf8, 98052);
            json.WriteEndObject();

            json.WriteStartArray(ExtraArrayUtf8);
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteNumberValue(_numberArrayValues[i]);
            }
            json.WriteEndArray();

            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }

        [Benchmark]
        public void WriteBasicUt16()
        {
            _arrayBufferWriter.Clear();
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = Formatted, SkipValidation = SkipValidation });
            var json = new Utf8JsonWriter(_arrayBufferWriter, state);

            json.WriteStartObject();
            json.WriteNumber("age", 42);
            json.WriteString("first", "John");
            json.WriteString("last", "Smith");
            json.WriteStartArray("phoneNumbers");
            json.WriteStringValue("425-000-1212");
            json.WriteStringValue("425-000-1213");
            json.WriteEndArray();
            json.WriteStartObject("address");
            json.WriteString("street", "1 Microsoft Way");
            json.WriteString("city", "Redmond");
            json.WriteNumber("zip", 98052);
            json.WriteEndObject();

            json.WriteStartArray("ExtraArray");
            for (int i = 0; i < DataSize; i++)
            {
                json.WriteNumberValue(_numberArrayValues[i]);
            }
            json.WriteEndArray();

            json.WriteEndObject();
            json.Flush(isFinalBlock: true);
        }
    }
}
