using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Buffers.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(char))]
    public class ReadOnlySequenceTests<T>
    {
        private const int Size = 10_000;

        private readonly T[] _array = ValuesGenerator.Array<T>(Size);
        private BufferSegment<T> _startSegment, _endSegment;
        private ReadOnlyMemory<T> _memory;

        [Benchmark]
        public int IterateTryGetArray() => IterateTryGet(new ReadOnlySequence<T>(_array));

        [Benchmark]
        public int IterateForEachArray() => IterateForEach(new ReadOnlySequence<T>(_array));

        [Benchmark]
        public int IterateGetPositionArray() => IterateGetPosition(new ReadOnlySequence<T>(_array));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstArray() => First(new ReadOnlySequence<T>(_array));

#if NETCOREAPP3_0
        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstSpanArray() => FirstSpan(new ReadOnlySequence<T>(_array));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstSpanMemory() => FirstSpan(new ReadOnlySequence<T>(_memory));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstSpanSingleSegment()
            => FirstSpan(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstSpanTenSegments()
            => FirstSpan(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int FirstSpan(ReadOnlySequence<T> sequence)
        {
            int consume = 0;

            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;
            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;
            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;
            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;

            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;
            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;
            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;
            consume += sequence.FirstSpan.Length; consume += sequence.FirstSpan.Length;

            return consume;
        }

        [GlobalSetup(Targets = new [] { nameof(FirstSpanSingleSegment) })]
        public void SetupFirstSpanSingleSegment() => SetupSingleSegment();

        [GlobalSetup(Targets = new [] { nameof(FirstSpanMemory) })]
        public void MemoryFirstSpanMemory() => MemorySegment();

        [GlobalSetup(Targets = new [] { nameof(FirstSpanTenSegments) })]
        public void SetupFirstSpanTenSegments() => SetupTenSegments();
#endif

        [Benchmark(OperationsPerInvoke = 10)]
        public long SliceArray() => Slice(new ReadOnlySequence<T>(_array));

        [Benchmark]
        public int IterateTryGetMemory() => IterateTryGet(new ReadOnlySequence<T>(new ReadOnlyMemory<T>(_array)));

        [Benchmark]
        public int IterateForEachMemory() => IterateForEach(new ReadOnlySequence<T>(new ReadOnlyMemory<T>(_array)));

        [Benchmark]
        public int IterateGetPositionMemory() => IterateGetPosition(new ReadOnlySequence<T>(new ReadOnlyMemory<T>(_array)));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstMemory() => First(new ReadOnlySequence<T>(_memory));

        [Benchmark(OperationsPerInvoke = 10)]
        public long SliceMemory() => Slice(new ReadOnlySequence<T>(new ReadOnlyMemory<T>(_array)));

        [GlobalSetup(Targets = new [] { nameof(IterateTryGetSingleSegment), nameof(IterateForEachSingleSegment), nameof(IterateGetPositionSingleSegment), nameof(FirstSingleSegment), nameof(SliceSingleSegment) })]
        public void SetupSingleSegment() => _startSegment = _endSegment = new BufferSegment<T>(new ReadOnlyMemory<T>(_array));

        [GlobalSetup(Targets = new [] { nameof(FirstMemory) })]
        public void MemorySegment() => _memory = new ReadOnlyMemory<T>(_array);

        [Benchmark]
        public int IterateTryGetSingleSegment()
            => IterateTryGet(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [Benchmark]
        public int IterateForEachSingleSegment()
            => IterateForEach(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [Benchmark]
        public int IterateGetPositionSingleSegment()
            => IterateGetPosition(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstSingleSegment()
            => First(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [Benchmark(OperationsPerInvoke = 10)]
        public long SliceSingleSegment()
            => Slice(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [GlobalSetup(Targets = new [] { nameof(IterateTryGetTenSegments), nameof(IterateForEachTenSegments), nameof(IterateGetPositionTenSegments), nameof(FirstTenSegments), nameof(SliceTenSegments) })]
        public void SetupTenSegments()
        {
            const int segmentsCount = 10;
            const int segmentSize = Size / segmentsCount;
            _startSegment = new BufferSegment<T>(new ReadOnlyMemory<T>(_array.Take(segmentSize).ToArray()));
            _endSegment = _startSegment;
            for (int i = 1; i < segmentsCount; i++)
            {
                _endSegment = _endSegment.Append(new ReadOnlyMemory<T>(_array.Skip(i * segmentSize).Take(Size / segmentsCount).ToArray()));
            }
        }

        [Benchmark]
        public int IterateTryGetTenSegments()
            => IterateTryGet(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [Benchmark]
        public int IterateForEachTenSegments()
            => IterateForEach(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [Benchmark]
        public int IterateGetPositionTenSegments()
            => IterateGetPosition(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstTenSegments()
            => First(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [Benchmark(OperationsPerInvoke = 10)]
        public long SliceTenSegments()
            => Slice(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [MethodImpl(MethodImplOptions.NoInlining)] // make sure that the method does not get inlined for any of the benchmarks and we compare apples to apples
        private int IterateTryGet(ReadOnlySequence<T> sequence)
        {
            int consume = 0;

            SequencePosition position = sequence.Start;
            while (sequence.TryGet(ref position, out var memory))
                consume += memory.Length;

            return consume;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int IterateForEach(ReadOnlySequence<T> sequence)
        {
            int consume = 0;

            foreach (ReadOnlyMemory<T> memory in sequence)
                consume += memory.Length;

            return consume;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int IterateGetPosition(ReadOnlySequence<T> sequence)
        {
            int consume = 0;

            SequencePosition position = sequence.Start;
            int offset = (int)(sequence.Length / 10);
            SequencePosition end = sequence.GetPosition(0, sequence.End);

            while (!position.Equals(end))
            {
                position = sequence.GetPosition(offset, position);
                consume += position.GetInteger();
            }

            return consume;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int First(ReadOnlySequence<T> sequence)
        {
            int consume = 0;

            consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length;

            consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length;

            return consume;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private long Slice(ReadOnlySequence<T> sequence)
        {
            long consume = 0;

            consume += sequence.Slice(Size / 10 * 0, Size / 10).Length; // 1
            consume += sequence.Slice(Size / 10 * 1, Size / 10).Length; // 2
            consume += sequence.Slice(Size / 10 * 2, Size / 10).Length; // 3
            consume += sequence.Slice(Size / 10 * 3, Size / 10).Length; // 4
            consume += sequence.Slice(Size / 10 * 4, Size / 10).Length; // 5
            consume += sequence.Slice(Size / 10 * 5, Size / 10).Length; // 6
            consume += sequence.Slice(Size / 10 * 6, Size / 10).Length; // 7
            consume += sequence.Slice(Size / 10 * 7, Size / 10).Length; // 8
            consume += sequence.Slice(Size / 10 * 8, Size / 10).Length; // 9
            consume += sequence.Slice(Size / 10 * 9, Size / 10).Length; // 10

            return consume;
        }
    }

    internal class BufferSegment<T> : ReadOnlySequenceSegment<T>
    {
        public BufferSegment(ReadOnlyMemory<T> memory) => Memory = memory;

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