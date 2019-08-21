// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Collections.Generic;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public class Perf_Segment
    {
        // Keep the JsonStrings resource names in sync with TestCaseType enum values.
        public enum TestCaseType
        {
            Json4KB,
            Json40KB,
            Json400KB
        }

        private string _jsonString;
        private byte[] _dataUtf8;
        private Dictionary<int, ReadOnlySequence<byte>> _sequences;
        private ReadOnlySequence<byte> _sequenceSingle;

        [ParamsAllValues]
        public TestCaseType TestCase;

        [GlobalSetup]
        public void Setup()
        {
            _jsonString = JsonStrings.ResourceManager.GetString(TestCase.ToString());

            _dataUtf8 = Encoding.UTF8.GetBytes(_jsonString);

            _sequenceSingle = new ReadOnlySequence<byte>(_dataUtf8);

            _sequences = new Dictionary<int, ReadOnlySequence<byte>>();

            foreach(int segmentSize in new int[] { 4_096, 8_192 })
            {
                _sequences.Add(segmentSize, Utf8JsonReaderCommentsTests.GetSequence(_dataUtf8, segmentSize));
            }
        }

        [Benchmark]
        public void ReadSingleSegmentSequence()
        {
            var json = new Utf8JsonReader(_sequenceSingle);
            while (json.Read()) ;
        }

        [Benchmark]
        [Arguments(4_096)]
        [Arguments(8_192)]
        public void ReadSingleSegmentSequenceByN(int numberOfBytes)
        {
            int consumed = 0;
            bool isFinalBlock = numberOfBytes >= _dataUtf8.Length;
            JsonReaderState jsonState = default;
            ReadOnlySpan<byte> fullDataAsSpan = _dataUtf8.AsSpan();

            while (consumed < _dataUtf8.Length)
            {
                ReadOnlySpan<byte>  data = isFinalBlock?
                    fullDataAsSpan.Slice(consumed) :
                    fullDataAsSpan.Slice(consumed, numberOfBytes);

                var json = new Utf8JsonReader(data, isFinalBlock, jsonState);

                while (json.Read()) ;

                consumed += (int)json.BytesConsumed;
                jsonState = json.CurrentState;

                if (consumed >= _dataUtf8.Length - numberOfBytes)
                {
                    isFinalBlock = true;
                }
            }
        }

        [Benchmark]
        [Arguments(4_096)]
        [Arguments(8_192)]
        public void ReadMultiSegmentSequence(int segmentSize)
        {
            var json = new Utf8JsonReader(_sequences[segmentSize]);
            while (json.Read()) ;
        }

        [Benchmark]
        [Arguments(4_096)]
        [Arguments(8_192)]
        public void ReadMultiSegmentSequenceUsingSpan(int segmentSize)
        {
            ReadOnlySequence<byte> sequenceMultiple = _sequences[segmentSize];

            byte[] buffer = ArrayPool<byte>.Shared.Rent(segmentSize * 2);
            JsonReaderState state = default;
            int previous = 0;
            int consumed = 0;
            foreach (ReadOnlyMemory<byte> memory in sequenceMultiple)
            {
                ReadOnlySpan<byte> span = memory.Span;
                Span<byte> bufferSpan = buffer;

                //Copy values of the new sequence to post-leftover locations.
                span.CopyTo(bufferSpan.Slice(previous));

                //Trim the buffer to the size of the leftover + the size of current sequence.
                bufferSpan = bufferSpan.Slice(0, span.Length + previous);

                bool isFinalBlock = consumed == sequenceMultiple.Length;

                var json = new Utf8JsonReader(bufferSpan, isFinalBlock, state);
                while (json.Read()) ;

                state = json.CurrentState;

                if (json.BytesConsumed < bufferSpan.Length)
                {
                    ReadOnlySpan<byte> leftover = bufferSpan.Slice((int)json.BytesConsumed);
                    previous = leftover.Length;
                    //Carry the leftover in order to read it on the next sequence.
                    leftover.CopyTo(buffer);
                }
                else
                {
                    previous = 0;
                }
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
