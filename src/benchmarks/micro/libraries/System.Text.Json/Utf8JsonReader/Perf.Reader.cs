// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.IO;
using System.Memory;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.JSON)]
    public class Perf_Reader
    {
        // Keep the JsonStrings resource names in sync with TestCaseType enum values.
        public enum TestCaseType
        {
            HelloWorld,
            DeepTree,
            BroadTree,
            LotsOfNumbers,
            LotsOfStrings,
            Json400B,
            Json4KB,
            Json40KB
        }

        private string _jsonString;
        private byte[] _dataUtf8;
        private ReadOnlySequence<byte> _sequence;
        private ReadOnlySequence<byte> _sequenceSingle;
        private byte[] _destination;

        [ParamsAllValues]
        public TestCaseType TestCase;

        [Params(true, false)]
        public bool IsDataCompact;

        [GlobalSetup]
        public void Setup()
        {
            _jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            // Remove all formatting/indendation
            if (IsDataCompact)
            {
                using (var jsonReader = new JsonTextReader(new StringReader(_jsonString)))
                using (var stringWriter = new StringWriter())
                using (var jsonWriter = new JsonTextWriter(stringWriter))
                {
                    JToken obj = JToken.ReadFrom(jsonReader);
                    obj.WriteTo(jsonWriter);
                    _jsonString = stringWriter.ToString();
                }
            }

            _dataUtf8 = Encoding.UTF8.GetBytes(_jsonString);

            ReadOnlyMemory<byte> dataMemory = _dataUtf8;
            _sequenceSingle = new ReadOnlySequence<byte>(dataMemory);

            var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, _dataUtf8.Length / 2));
            ReadOnlyMemory<byte> secondMem = dataMemory.Slice(_dataUtf8.Length / 2);
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
            _sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);
            
            _destination = new byte[_dataUtf8.Length * 2];
        }

        [Benchmark]
        public void ReadSpanEmptyLoop()
        {
            var json = new Utf8JsonReader(_dataUtf8);
            while (json.Read()) ;
        }

        [Benchmark]
        public void ReadSingleSpanSequenceEmptyLoop()
        {
            var json = new Utf8JsonReader(_sequenceSingle);
            while (json.Read()) ;
        }

        [Benchmark]
        public void ReadMultiSpanSequenceEmptyLoop()
        {
            var json = new Utf8JsonReader(_sequence);
            while (json.Read()) ;
        }

        [Benchmark]
        public byte[] ReadReturnBytes()
        {
            Span<byte> destination = _destination;
            var json = new Utf8JsonReader(_dataUtf8);
            while (json.Read())
            {
                JsonTokenType tokenType = json.TokenType;
                ReadOnlySpan<byte> valueSpan = json.ValueSpan;
                switch (tokenType)
                {
                    case JsonTokenType.PropertyName:
                        valueSpan.CopyTo(destination);
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.Number:
                    case JsonTokenType.String:
                        valueSpan.CopyTo(destination);
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.True:
                        // Special casing True/False so that the casing matches with Json.NET
                        destination[0] = (byte)'T';
                        destination[1] = (byte)'r';
                        destination[2] = (byte)'u';
                        destination[3] = (byte)'e';
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.False:
                        destination[0] = (byte)'F';
                        destination[1] = (byte)'a';
                        destination[2] = (byte)'l';
                        destination[3] = (byte)'s';
                        destination[4] = (byte)'e';
                        destination[valueSpan.Length] = (byte)',';
                        destination[valueSpan.Length + 1] = (byte)' ';
                        destination = destination.Slice(valueSpan.Length + 2);
                        break;
                    case JsonTokenType.Null:
                        // Special casing Null so that it matches what JSON.NET does
                        break;
                    default:
                        break;
                }
            }
            return _destination;
        }
    }
}
