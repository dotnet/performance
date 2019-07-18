// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace MicroBenchmarks.corefx
{
    internal class BufferSegment<T> : ReadOnlySequenceSegment<T>
    {
        public BufferSegment(System.ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

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
    public partial class ReadOnlySequenceBenchmarks
    {
        private ReadOnlySequence<byte> _sequence;
        private ReadOnlySequence<byte> _multiSegmentSequence;
        private System.SequencePosition _start;
        private System.SequencePosition _end;
        private System.SequencePosition _ms_start;
        private System.SequencePosition _ms_end;

        [GlobalSetup]
        public void GlobalSetup()
        {
            System.Memory<byte> memory = new System.Memory<byte>(Enumerable.Repeat((byte)1, 10000).ToArray());
            _sequence = new ReadOnlySequence<byte>(memory);
            _start =_sequence.GetPosition(10);
            _end =_sequence.GetPosition(9990);

            BufferSegment<byte> firstSegment = new BufferSegment<byte>(memory.Slice(0, memory.Length / 2 ));
            BufferSegment<byte> secondSegment = firstSegment.Append(memory.Slice(memory.Length / 2 , memory.Length / 2 ));
            _multiSegmentSequence = new ReadOnlySequence<byte>(firstSegment, 0, secondSegment, firstSegment.Memory.Length);
            _ms_start = _multiSegmentSequence.GetPosition(10);
            _ms_end = _multiSegmentSequence.GetPosition(9990);
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

        // Add multiple calls for Slice(startPosition, endPosition)
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

        // MultiSegment Benchmarks
        [Benchmark]
        public ReadOnlySequence<byte> MS_StartPosition() => _multiSegmentSequence.Slice(_ms_start);

        [Benchmark]
        public ReadOnlySequence<byte> MS_Start() => _multiSegmentSequence.Slice(0);

        [Benchmark]
        public ReadOnlySequence<byte> MS_Start_And_Length() => _multiSegmentSequence.Slice(0, 10000);

        [Benchmark]
        public ReadOnlySequence<byte> MS_Start_And_EndPosition() => _multiSegmentSequence.Slice(0, _ms_end);

        [Benchmark]
        public ReadOnlySequence<byte> MS_StartPosition_And_Length() => _multiSegmentSequence.Slice(_ms_start, 3);

        [Benchmark]
        public ReadOnlySequence<byte> MS_StartPosition_And_EndPosition() => _multiSegmentSequence.Slice(_ms_start, _ms_end);

        [Benchmark]
        public ReadOnlySequence<byte> MS_RepeatSlice()
        {
            var localSequence = _multiSegmentSequence.Slice(0, 10000);
            localSequence = localSequence.Slice(0, 5000);
            localSequence = localSequence.Slice(0, 2500);
            localSequence = localSequence.Slice(0, 1250);
            localSequence = localSequence.Slice(0, 625);
            return localSequence;
        }

        // Add multiple calls for Slice(startPosition, endPosition)
        [Benchmark]
        public ReadOnlySequence<byte> MS_RepeatSlice_StartPosition_And_EndPosition()
        {
            var localSequence = _multiSegmentSequence.Slice(_ms_start, _ms_end);
            localSequence = localSequence.Slice(_ms_start, localSequence.End);
            localSequence = localSequence.Slice(_ms_start, localSequence.End);
            localSequence = localSequence.Slice(_ms_start, localSequence.End);
            localSequence = localSequence.Slice(_ms_start, localSequence.End);
            return localSequence;
        }
    }
}
