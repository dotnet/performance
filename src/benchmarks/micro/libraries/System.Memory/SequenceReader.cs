using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.Buffers;
using System.Linq;

namespace System.Memory
{
    [BenchmarkCategory(Categories.Libraries)]
    public class SequenceReader
    {
        private const int Count = 10000;

        private ReadOnlySequence<int> _multiSegmentSequence;

        [GlobalSetup]
        public void Setup()
        {
            System.Memory<int> memory = new System.Memory<int>(Enumerable.Range(1, Count).ToArray());

            BufferSegment<int> firstSegment = new BufferSegment<int>(memory.Slice(0, memory.Length / 2));
            BufferSegment<int> secondSegment = firstSegment.Append(memory.Slice(memory.Length / 2, memory.Length / 2));

            _multiSegmentSequence = new ReadOnlySequence<int>(firstSegment, 0, secondSegment, firstSegment.Memory.Length);
        }

        [Benchmark(OperationsPerInvoke = 16)]
        public ReadOnlySequence<int> TryReadTo()
        {
            var sequenceReader = new System.Buffers.SequenceReader<int>(_multiSegmentSequence);

            ReadOnlySequence<int> outSequence;

            sequenceReader.TryReadTo(out outSequence, Count / 16); sequenceReader.TryReadTo(out outSequence, Count / 15);
            sequenceReader.TryReadTo(out outSequence, Count / 14); sequenceReader.TryReadTo(out outSequence, Count / 13);
            sequenceReader.TryReadTo(out outSequence, Count / 12); sequenceReader.TryReadTo(out outSequence, Count / 11);
            sequenceReader.TryReadTo(out outSequence, Count / 10); sequenceReader.TryReadTo(out outSequence, Count / 09);
            sequenceReader.TryReadTo(out outSequence, Count / 08); sequenceReader.TryReadTo(out outSequence, Count / 07);
            sequenceReader.TryReadTo(out outSequence, Count / 06); sequenceReader.TryReadTo(out outSequence, Count / 05);
            sequenceReader.TryReadTo(out outSequence, Count / 04); sequenceReader.TryReadTo(out outSequence, Count / 03);
            sequenceReader.TryReadTo(out outSequence, Count / 02); sequenceReader.TryReadTo(out outSequence, Count / 01);

            return outSequence;
        }
    }
}
