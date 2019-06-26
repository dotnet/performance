using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroBenchmarks.corefx
{
    public partial class ReadOnlySequenceBenchmarks
    {
#if NETCOREAPP3_0
        [Benchmark]
        public void BenchmarkSequenceReader()
        {
            System.Memory<int> memory = new System.Memory<int>(Enumerable.Range((int)1, 10000).ToArray());
            var sequence = new ReadOnlySequence<int>(memory);

            BufferSegment<int> firstSegment = new BufferSegment<int>(memory.Slice(0, memory.Length / 2));
            BufferSegment<int> secondSegment = firstSegment.Append(memory.Slice(memory.Length / 2, memory.Length / 2));
            var multiSegmentSequence = new ReadOnlySequence<int>(firstSegment, 0, secondSegment, firstSegment.Memory.Length);

            var sequenceReader = new System.Buffers.SequenceReader<int>(multiSegmentSequence);

            sequenceReader.TryReadTo(out ReadOnlySequence<int> outSequence, (int)10000);
            sequenceReader.TryReadTo(out outSequence, (int)7500);
            sequenceReader.TryReadTo(out outSequence, (int)5000);
            sequenceReader.TryReadTo(out outSequence, (int)2500);
            sequenceReader.TryReadTo(out outSequence, (int)1250);
        }

#endif
    }
}
