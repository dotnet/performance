using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Benchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(string))]
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class Constructors<T>
    {
        private const int Size = 10;

        private T[] _nonEmptyArray;
        private ArraySegment<T> _arraySegment;
        private IntPtr _validPointer;
        private T _field;

        public unsafe Constructors()
        {
            _nonEmptyArray = new T[Size];
            _arraySegment = new ArraySegment<T>(_nonEmptyArray, 0, Size);
            _field = _nonEmptyArray[0];
            _validPointer = (IntPtr) Unsafe.AsPointer(ref _field);
        }

        [Benchmark(Baseline = true)]
        public Span<T> SpanFromArray() => new Span<T>(_nonEmptyArray);

        [Benchmark]
        public ReadOnlySpan<T> ReadOnlySpanFromArray() => new ReadOnlySpan<T>(_nonEmptyArray);

        [Benchmark]
        public Span<T> SpanFromArrayStartLength() => new Span<T>(_nonEmptyArray, start: 0, length: Size);

        [Benchmark]
        public ReadOnlySpan<T> ReadOnlySpanFromArrayStartLength() => new ReadOnlySpan<T>(_nonEmptyArray, start: 0, length: Size);

        [Benchmark]
        public unsafe Span<T> SpanFromPointerLength() => new Span<T>(_validPointer.ToPointer(), Size);

        [Benchmark]
        public unsafe ReadOnlySpan<T> ReadOnlyFromPointerLength() => new ReadOnlySpan<T>(_validPointer.ToPointer(), Size);

        [Benchmark]
        public Span<T> SpanImplicitCastFromArray() => _nonEmptyArray;

        [Benchmark]
        public ReadOnlySpan<T> ReadOnlySpanImplicitCastFromArray() => _nonEmptyArray;

        [Benchmark]
        public Span<T> SpanImplicitCastFromArraySegment() => _arraySegment;

        [Benchmark]
        public ReadOnlySpan<T> ReadOnlySpanImplicitCastFromArraySegment() => _arraySegment;

        [Benchmark]
        public ReadOnlySpan<T> ReadOnlySpanImplicitCastFromSpan() => Span<T>.Empty;

        [Benchmark]
        public Memory<T> MemoryFromArray() => new Memory<T>(_nonEmptyArray);

        [Benchmark]
        public ReadOnlyMemory<T> ReadOnlyMemoryFromArray() => new ReadOnlyMemory<T>(_nonEmptyArray);

        [Benchmark]
        public Memory<T> MemoryFromArrayStartLength() => new Memory<T>(_nonEmptyArray, start: 0, length: Size);

        [Benchmark]
        public ReadOnlyMemory<T> ReadOnlyMemoryFromArrayStartLength() => new ReadOnlyMemory<T>(_nonEmptyArray, start: 0, length: Size);

#if NETCOREAPP2_1 // netcoreapp specific API https://github.com/dotnet/coreclr/issues/16126
        [Benchmark]
        public Span<T> MemoryMarshalCreateSpan() => MemoryMarshal.CreateSpan<T>(ref _field, Size);
    
        [Benchmark]
        public ReadOnlySpan<T> MemoryMarshalCreateReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan<T>(ref _field, Size);
#endif
    }
}