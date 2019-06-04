// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using MicroBenchmarks.Serializers;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    public partial class Utf8JsonReaderTests
    {
        private ReadOnlySequence<byte> s_singleEmptyMultiLineCommentSequence1;
        private ReadOnlySequence<byte> s_multipleEmptyMultiLineCommentSequence1;
        private ReadOnlySequence<byte> s_singleShortMultiLineCommentSequence1;
        private ReadOnlySequence<byte> s_multipleShortMultiLineCommentSequence1;
        private ReadOnlySequence<byte> s_singleLongMultiLineComment1Sequence1;
        private ReadOnlySequence<byte> s_singleLongMultiLineComment2Sequence1;

        private ReadOnlySequence<byte> s_singleEmptySingleLineCommentSequence1;
        private ReadOnlySequence<byte> s_multipleEmptySingleLineCommentSequence1;
        private ReadOnlySequence<byte> s_singleShortSingleLineCommentSequence1;
        private ReadOnlySequence<byte> s_multipleShortSingleLineCommentSequence1;
        private ReadOnlySequence<byte> s_singleLongSingleLineCommentSequence1;

        private ReadOnlySequence<byte> s_singleEmptyMultiLineCommentSequence100;
        private ReadOnlySequence<byte> s_multipleEmptyMultiLineCommentSequence100;
        private ReadOnlySequence<byte> s_singleShortMultiLineCommentSequence100;
        private ReadOnlySequence<byte> s_multipleShortMultiLineCommentSequence100;
        private ReadOnlySequence<byte> s_singleLongMultiLineComment1Sequence100;
        private ReadOnlySequence<byte> s_singleLongMultiLineComment2Sequence100;

        private ReadOnlySequence<byte> s_singleEmptySingleLineCommentSequence100;
        private ReadOnlySequence<byte> s_multipleEmptySingleLineCommentSequence100;
        private ReadOnlySequence<byte> s_singleShortSingleLineCommentSequence100;
        private ReadOnlySequence<byte> s_multipleShortSingleLineCommentSequence100;
        private ReadOnlySequence<byte> s_singleLongSingleLineCommentSequence100;

        private void MultiSegmentSetup()
        {
            // called within SingleSegmentSetup

            s_singleEmptyMultiLineCommentSequence1 = GetSequence(s_singleEmptyMultiLineComment, 1);
            s_multipleEmptyMultiLineCommentSequence1 = GetSequence(s_multipleEmptyMultiLineComment, 1);
            s_singleShortMultiLineCommentSequence1 = GetSequence(s_singleShortMultiLineComment, 1);
            s_multipleShortMultiLineCommentSequence1 = GetSequence(s_multipleShortMultiLineComment, 1);
            s_singleLongMultiLineComment1Sequence1 = GetSequence(s_singleLongMultiLineComment1, 1);
            s_singleLongMultiLineComment2Sequence1 = GetSequence(s_singleLongMultiLineComment2, 1);

            s_singleEmptySingleLineCommentSequence1 = GetSequence(s_singleEmptySingleLineComment, 1);
            s_multipleEmptySingleLineCommentSequence1 = GetSequence(s_multipleEmptySingleLineComment, 1);
            s_singleShortSingleLineCommentSequence1 = GetSequence(s_singleShortSingleLineComment, 1);
            s_multipleShortSingleLineCommentSequence1 = GetSequence(s_multipleShortSingleLineComment, 1);
            s_singleLongSingleLineCommentSequence1 = GetSequence(s_singleLongSingleLineComment, 1);

            s_singleEmptyMultiLineCommentSequence100 = GetSequence(s_singleEmptyMultiLineComment, 100);
            s_multipleEmptyMultiLineCommentSequence100 = GetSequence(s_multipleEmptyMultiLineComment, 100);
            s_singleShortMultiLineCommentSequence100 = GetSequence(s_singleShortMultiLineComment, 100);
            s_multipleShortMultiLineCommentSequence100 = GetSequence(s_multipleShortMultiLineComment, 100);
            s_singleLongMultiLineComment1Sequence100 = GetSequence(s_singleLongMultiLineComment1, 100);
            s_singleLongMultiLineComment2Sequence100 = GetSequence(s_singleLongMultiLineComment2, 100);

            s_singleEmptySingleLineCommentSequence100 = GetSequence(s_singleEmptySingleLineComment, 100);
            s_multipleEmptySingleLineCommentSequence100 = GetSequence(s_multipleEmptySingleLineComment, 100);
            s_singleShortSingleLineCommentSequence100 = GetSequence(s_singleShortSingleLineComment, 100);
            s_multipleShortSingleLineCommentSequence100 = GetSequence(s_multipleShortSingleLineComment, 100);
            s_singleLongSingleLineCommentSequence100 = GetSequence(s_singleLongSingleLineComment, 100);
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptyMultiLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleEmptyMultiLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptyMultiLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleEmptyMultiLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortMultiLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleShortMultiLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortMultiLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleShortMultiLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment1Sequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment1Sequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment2Sequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment2Sequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptySingleLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleEmptySingleLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptySingleLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleEmptySingleLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortSingleLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleShortSingleLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortSingleLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleShortSingleLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongSingleLineCommentSequence1_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleLongSingleLineCommentSequence1, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptyMultiLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleEmptyMultiLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptyMultiLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleEmptyMultiLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortMultiLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleShortMultiLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortMultiLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleShortMultiLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment1Sequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment1Sequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment2Sequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment2Sequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptySingleLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleEmptySingleLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptySingleLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleEmptySingleLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortSingleLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleShortSingleLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortSingleLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_multipleShortSingleLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongSingleLineCommentSequence100_CommentHandling_Allow() => MultiSegment_GenericReadTest(s_singleLongSingleLineCommentSequence100, JsonCommentHandling.Allow);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptyMultiLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleEmptyMultiLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptyMultiLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleEmptyMultiLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortMultiLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleShortMultiLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortMultiLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleShortMultiLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment1Sequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment1Sequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment2Sequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment2Sequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptySingleLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleEmptySingleLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptySingleLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleEmptySingleLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortSingleLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleShortSingleLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortSingleLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleShortSingleLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongSingleLineCommentSequence1_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleLongSingleLineCommentSequence1, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptyMultiLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleEmptyMultiLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptyMultiLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleEmptyMultiLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortMultiLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleShortMultiLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortMultiLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleShortMultiLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment1Sequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment1Sequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongMultiLineComment2Sequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleLongMultiLineComment2Sequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleEmptySingleLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleEmptySingleLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleEmptySingleLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleEmptySingleLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleShortSingleLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleShortSingleLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_MultipleShortSingleLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_multipleShortSingleLineCommentSequence100, JsonCommentHandling.Skip);

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void MultiSegment_SingleLongSingleLineCommentSequence100_CommentHandling_Skip() => MultiSegment_GenericReadTest(s_singleLongSingleLineCommentSequence100, JsonCommentHandling.Skip);

        private static void MultiSegment_GenericReadTest(ReadOnlySequence<byte> data, JsonCommentHandling commentHandling)
        {
            for (int i = 0; i < Iterations; i++)
            {
                var state = new JsonReaderState(options: new JsonReaderOptions { CommentHandling = commentHandling });
                var json = new Utf8JsonReader(data, isFinalBlock: true, state);

                while (json.Read())
                {
                }
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
            foreach (var buffer in buffers)
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
    }
}