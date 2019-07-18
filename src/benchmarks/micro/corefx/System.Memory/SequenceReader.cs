using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Linq;

namespace System.Memory
{
    public class SequenceReader
    {
        private ReadOnlySequence<int> _multiSegmentSequence;

        [GlobalSetup]
        public void Setup()
        {
            System.Memory<int> memory = new System.Memory<int>(Enumerable.Range((int)1, 10000).ToArray());

            BufferSegment<int> firstSegment = new BufferSegment<int>(memory.Slice(0, memory.Length / 2));
            BufferSegment<int> secondSegment = firstSegment.Append(memory.Slice(memory.Length / 2, memory.Length / 2));

            _multiSegmentSequence = new ReadOnlySequence<int>(firstSegment, 0, secondSegment, firstSegment.Memory.Length);
        }

        [Benchmark]
        public void TryReadTo()
        {
            var sequenceReader = new System.Buffers.SequenceReader<int>(_multiSegmentSequence);

            sequenceReader.TryReadTo(out ReadOnlySequence<int> outSequence, (int)10000);
            sequenceReader.TryReadTo(out outSequence, (int)7500);
            sequenceReader.TryReadTo(out outSequence, (int)5000);
            sequenceReader.TryReadTo(out outSequence, (int)2500);
            sequenceReader.TryReadTo(out outSequence, (int)1250);
        }
    }
}
