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

        [Benchmark]
        public int IterateTryGetOverArray() => IterateTryGet(new ReadOnlySequence<T>(_array));

        [Benchmark]
        public int IterateForEachOverArray() => IterateForEach(new ReadOnlySequence<T>(_array));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstArray() => First(new ReadOnlySequence<T>(_array));

        [Benchmark]
        public int IterateTryGetOverMemory() => IterateTryGet(new ReadOnlySequence<T>(new ReadOnlyMemory<T>(_array)));

        [Benchmark]
        public int IterateForEachOverMemory() => IterateForEach(new ReadOnlySequence<T>(new ReadOnlyMemory<T>(_array)));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstMemory() => First(new ReadOnlySequence<T>(new ReadOnlyMemory<T>(_array)));

        [GlobalSetup(Targets = new [] { nameof(IterateTryGetOverSingleSegment), nameof(IterateForEachOverSingleSegment), nameof(FirstSingleSegment) })]
        public void SetupSingleSegment() => _startSegment = _endSegment = new BufferSegment<T>(new ReadOnlyMemory<T>(_array));

        [Benchmark]
        public int IterateTryGetOverSingleSegment()
            => IterateTryGet(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [Benchmark]
        public int IterateForEachOverSingleSegment()
            => IterateForEach(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstSingleSegment()
            => First(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size));

        [GlobalSetup(Targets = new [] { nameof(IterateTryGetOverTenSegments), nameof(IterateForEachOverTenSegments), nameof(FirstTenSegments) })]
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
        public int IterateTryGetOverTenSegments()
            => IterateTryGet(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [Benchmark]
        public int IterateForEachOverTenSegments()
            => IterateForEach(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

        [Benchmark(OperationsPerInvoke = 16)]
        public int FirstTenSegments()
            => First(new ReadOnlySequence<T>(startSegment: _startSegment, startIndex: 0, endSegment: _endSegment, endIndex: Size / 10));

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
        private int First(ReadOnlySequence<T> sequence)
        {
            int consume = 0;

            consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length;
            consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length; consume += sequence.First.Length;

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