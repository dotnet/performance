// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace System.Text.Json.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.JSON)]
    public partial class Utf8JsonReader_Comments
    {
        [Params(JsonCommentHandling.Skip, JsonCommentHandling.Allow)]
        public JsonCommentHandling CommentHandling;

        [Params(true, false)]
        public bool SingleSegment;

        private byte[] _jsonPayload;
        private ReadOnlySequence<byte> _jsonPayloadSequence;

        [ParamsSource(nameof(TestCaseValues))]
        public TestCaseType TestCase;

        public static IEnumerable<TestCaseType> TestCaseValues() => (IEnumerable<TestCaseType>)Enum.GetValues(typeof(TestCaseType));

        private static Dictionary<TestCaseType, string> s_testCases = new Dictionary<TestCaseType, string>()
        {
            { TestCaseType.ShortMultiLine, "{}/*asdasd*/" },
            { TestCaseType.LongMultiLine, "{}/*" + string.Join("", Enumerable.Repeat("some comment\n", 1000)) + "*/" },
            { TestCaseType.ShortSingleLine, "{}//asdasd\n" },
            { TestCaseType.LongSingleLine, "{}//" + new string('c', 2000) + "\n" },
        };

        [GlobalSetup]
        public void Setup()
        {
            _jsonPayload = Encoding.UTF8.GetBytes(s_testCases[TestCase]);
            _jsonPayloadSequence = GetSequence(_jsonPayload, 100);
        }

        [Benchmark]
        public void EmptyLoop()
        {
            var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = CommentHandling });
            Utf8JsonReader reader = !SingleSegment ?
                new Utf8JsonReader(_jsonPayloadSequence, isFinalBlock: true, state) :
                new Utf8JsonReader(_jsonPayload, isFinalBlock: true, state);

            while (reader.Read())
            {
            }
        }

        private static ReadOnlySequence<byte> GetSequence(byte[] dataUtf8, int segmentSize)
        {
            int numberOfSegments = dataUtf8.Length / segmentSize + 1;
            byte[][] buffers = new byte[numberOfSegments][];

            for (int j = 0; j < numberOfSegments - 1; j++)
            {
                buffers[j] = new byte[segmentSize];
                Array.Copy(dataUtf8, j * segmentSize, buffers[j], 0, segmentSize);
            }

            int remaining = dataUtf8.Length % segmentSize;
            buffers[numberOfSegments - 1] = new byte[remaining];
            Array.Copy(dataUtf8, dataUtf8.Length - remaining, buffers[numberOfSegments - 1], 0, remaining);

            return CreateReadOnlySequence(buffers);
        }

        private static ReadOnlySequence<byte> CreateReadOnlySequence(params byte[][] buffers)
        {
            if (buffers.Length == 1)
                return new ReadOnlySequence<byte>(buffers[0]);

            var list = new List<Memory<byte>>();

            foreach (byte[] buffer in buffers)
                list.Add(buffer);

            return ReadOnlyBufferSegment.Create(list.ToArray());
        }

        private class ReadOnlyBufferSegment : ReadOnlySequenceSegment<byte>
        {
            public static ReadOnlySequence<byte> Create(IEnumerable<Memory<byte>> buffers)
            {
                ReadOnlyBufferSegment segment = null;
                ReadOnlyBufferSegment first = null;
                foreach (Memory<byte> buffer in buffers)
                {
                    var newSegment = new ReadOnlyBufferSegment()
                    {
                        Memory = buffer,
                    };

                    if (segment != null)
                    {
                        segment.Next = newSegment;
                        newSegment.RunningIndex = segment.RunningIndex + segment.Memory.Length;
                    }
                    else
                    {
                        first = newSegment;
                    }

                    segment = newSegment;
                }

                if (first == null)
                {
                    first = segment = new ReadOnlyBufferSegment();
                }

                return new ReadOnlySequence<byte>(first, 0, segment, segment.Memory.Length);
            }
        }

        public enum TestCaseType
        {
            ShortMultiLine,
            LongMultiLine,
            ShortSingleLine,
            LongSingleLine,
        }
    }
}