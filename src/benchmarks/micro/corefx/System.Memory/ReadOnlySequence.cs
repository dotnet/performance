// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Linq;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Memory
{
    internal class BufferSegment<T> : ReadOnlySequenceSegment<T>
    {
        public BufferSegment(System.ReadOnlyMemory<T> memory) => Memory = memory;

        public BufferSegment<T> Append(System.ReadOnlyMemory<T> memory)
        {
            var segment = new BufferSegment<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };
            Next = segment;
            return segment;
        }
    }

    [BenchmarkCategory(Categories.CoreFX)]
    public class ReadOnlySequence
    {
        public enum SequenceKind { Single, Multiple };

        [Params(SequenceKind.Single, SequenceKind.Multiple)]
        public SequenceKind Segment { get; set; }

        private ReadOnlySequence<byte> _sequence;
        private SequencePosition _start;
        private SequencePosition _end;

        [GlobalSetup]
        public void GlobalSetup()
        {
            System.Memory<byte> memory = new System.Memory<byte>(Enumerable.Repeat((byte)1, 10000).ToArray());

            if (Segment == SequenceKind.Single)
            {
                _sequence = new ReadOnlySequence<byte>(memory);
                _start = _sequence.GetPosition(10);
                _end = _sequence.GetPosition(9990);
            }
            else
            {
                BufferSegment<byte> firstSegment = new BufferSegment<byte>(memory.Slice(0, memory.Length / 2));
                BufferSegment<byte> secondSegment = firstSegment.Append(memory.Slice(memory.Length / 2, memory.Length / 2));
                _sequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, firstSegment.Memory.Length);
                _start = _sequence.GetPosition(10);
                _end = _sequence.GetPosition(9990);
            }
        }

        [Benchmark]
        public ReadOnlySequence<byte> StartPosition() => _sequence.Slice(_start);

        [Benchmark]
        public ReadOnlySequence<byte> Start() => _sequence.Slice(0);

        [Benchmark]
        public ReadOnlySequence<byte> Start_And_Length() => _sequence.Slice(0, 10000);

        [Benchmark]
        public ReadOnlySequence<byte> Start_And_EndPosition() => _sequence.Slice(0, _end);

        [Benchmark]
        public ReadOnlySequence<byte> StartPosition_And_Length() => _sequence.Slice(_start, 3);

        [Benchmark]
        public ReadOnlySequence<byte> StartPosition_And_EndPosition() => _sequence.Slice(_start, _end);

        [Benchmark]
        public ReadOnlySequence<byte> RepeatSlice()
        {
            var localSequence = _sequence.Slice(0, 10000);
            localSequence = localSequence.Slice(0, 5000);
            localSequence = localSequence.Slice(0, 2500);
            localSequence = localSequence.Slice(0, 1250);
            localSequence = localSequence.Slice(0, 625);
            return localSequence;
        }

        [Benchmark]
        public ReadOnlySequence<byte> RepeatSlice_StartPosition_And_EndPosition()
        {
            var localSequence = _sequence.Slice(_start, _end);
            localSequence = localSequence.Slice(_start, localSequence.End);
            localSequence = localSequence.Slice(_start, localSequence.End);
            localSequence = localSequence.Slice(_start, localSequence.End);
            localSequence = localSequence.Slice(_start, localSequence.End);
            return localSequence;
        }
    }
}
