// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
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
        private MemoryStream _stream;
        private StreamReader _reader;

        [ParamsSource(nameof(TestCaseValues))]
        public TestCaseType TestCase;

        [Params(true, false)]
        public bool IsDataCompact;

        public static IEnumerable<TestCaseType> TestCaseValues() => (IEnumerable<TestCaseType>)Enum.GetValues(typeof(TestCaseType));

        [GlobalSetup]
        public void Setup()
        {
            _jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            // Remove all formatting/indendation
            if (IsDataCompact)
            {
                using (var jsonReader = new JsonTextReader(new StringReader(_jsonString)))
                {
                    JToken obj = JToken.ReadFrom(jsonReader);
                    var stringWriter = new StringWriter();
                    using (var jsonWriter = new JsonTextWriter(stringWriter))
                    {
                        obj.WriteTo(jsonWriter);
                        _jsonString = stringWriter.ToString();
                    }
                }
            }

            _dataUtf8 = Encoding.UTF8.GetBytes(_jsonString);

            ReadOnlyMemory<byte> dataMemory = _dataUtf8;
            _sequenceSingle = new ReadOnlySequence<byte>(dataMemory);

            var firstSegment = new BufferSegment<byte>(dataMemory.Slice(0, _dataUtf8.Length / 2));
            ReadOnlyMemory<byte> secondMem = dataMemory.Slice(_dataUtf8.Length / 2);
            BufferSegment<byte> secondSegment = firstSegment.Append(secondMem);
            _sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, secondMem.Length);

            _stream = new MemoryStream(_dataUtf8);
            _reader = new StreamReader(_stream, Encoding.UTF8, false, 1024, true);
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
            var outputArray = new byte[_dataUtf8.Length * 2];

            Span<byte> destination = outputArray;
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
            return outputArray;
        }
    }

    internal class BufferSegment<T> : ReadOnlySequenceSegment<T>
    {
        public BufferSegment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public BufferSegment<T> Append(ReadOnlyMemory<T> memory)
        {
            var segment = new BufferSegment<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }
}
