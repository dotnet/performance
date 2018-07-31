using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(string))]
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class Slice<T>
    {
        private const int Size = 10;

        private T[] _nonEmptyArray = new T[Size];

        [Benchmark(OperationsPerInvoke = 16)]
        public System.Span<T> SpanStart()
        {
            System.Span<T> span, slice;
            span = new System.Span<T>(_nonEmptyArray);

            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);

            return slice;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.Span<T> SpanStartLength()
        {
            System.Span<T> span, slice;
            span = new System.Span<T>(_nonEmptyArray);

            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);

            return slice;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlySpan<T> ReadOnlySpanStart()
        {
            System.ReadOnlySpan<T> span, slice;
            span = new System.ReadOnlySpan<T>(_nonEmptyArray);

            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);
            slice = span.Slice(Size / 2); Consume(in slice); slice = span.Slice(Size / 2); Consume(in slice);

            return slice;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlySpan<T> ReadOnlySpanStartLength()
        {
            System.ReadOnlySpan<T> span, slice;
            span = new System.ReadOnlySpan<T>(_nonEmptyArray);

            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);
            slice = span.Slice(Size / 2, 1); Consume(in slice); slice = span.Slice(Size / 2, 1); Consume(in slice);

            return slice;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.Memory<T> MemoryStart()
        {
            System.Memory<T> memory, slice;
            memory = new System.Memory<T>(_nonEmptyArray);

            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);

            return slice;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.Memory<T> MemoryStartLength()
        {
            System.Memory<T> memory, slice;
            memory = new System.Memory<T>(_nonEmptyArray);

            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);

            return slice;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlyMemory<T> ReadOnlyMemoryStart()
        {
            System.ReadOnlyMemory<T> memory, slice;
            memory = new System.ReadOnlyMemory<T>(_nonEmptyArray);

            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);
            slice = memory.Slice(Size / 2); Consume(in slice); slice = memory.Slice(Size / 2); Consume(in slice);

            return slice;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlyMemory<T> ReadOnlyMemoryStartLength()
        {
            System.ReadOnlyMemory<T> memory, slice;
            memory = new System.ReadOnlyMemory<T>(_nonEmptyArray);

            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);
            slice = memory.Slice(Size / 2, 1); Consume(in slice); slice = memory.Slice(Size / 2, 1); Consume(in slice);

            return slice;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(in System.Span<T> _) { }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(in System.ReadOnlySpan<T> _) { }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(in System.Memory<T> _) { }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(in System.ReadOnlyMemory<T> _) { }
    }
}