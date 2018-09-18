// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private System.Memory<T> _memory;
        private System.ReadOnlyMemory<T> _readOnlyMemory;

        public unsafe Constructors()
        {
            _nonEmptyArray = new T[Size];
            _arraySegment = new ArraySegment<T>(_nonEmptyArray, 0, Size);
            _field = _nonEmptyArray[0];
            _validPointer = (IntPtr) Unsafe.AsPointer(ref _field);
            _memory = new System.Memory<T>(_nonEmptyArray);
            _readOnlyMemory = new System.ReadOnlyMemory<T>(_nonEmptyArray);
        }

        [Benchmark(Baseline = true)]
        public System.Span<T> SpanFromArray() => new System.Span<T>(_nonEmptyArray);

        [Benchmark]
        public System.ReadOnlySpan<T> ReadOnlySpanFromArray() => new System.ReadOnlySpan<T>(_nonEmptyArray);

        [Benchmark]
        public System.Span<T> SpanFromArrayStartLength() => new System.Span<T>(_nonEmptyArray, start: 0, length: Size);

        [Benchmark]
        public System.ReadOnlySpan<T> ReadOnlySpanFromArrayStartLength() => new System.ReadOnlySpan<T>(_nonEmptyArray, start: 0, length: Size);

        [Benchmark]
        public unsafe System.Span<T> SpanFromPointerLength() => new System.Span<T>(_validPointer.ToPointer(), Size);

        [Benchmark]
        public unsafe System.ReadOnlySpan<T> ReadOnlyFromPointerLength() => new System.ReadOnlySpan<T>(_validPointer.ToPointer(), Size);

        [Benchmark]
        public System.Span<T> SpanFromMemory() => _memory.Span;

        [Benchmark]
        public System.ReadOnlySpan<T> ReadOnlySpanFromMemory() => _readOnlyMemory.Span;

        [Benchmark]
        public System.Span<T> SpanImplicitCastFromArray() => _nonEmptyArray;

        [Benchmark]
        public System.ReadOnlySpan<T> ReadOnlySpanImplicitCastFromArray() => _nonEmptyArray;

        [Benchmark]
        public System.Span<T> SpanImplicitCastFromArraySegment() => _arraySegment;

        [Benchmark]
        public System.ReadOnlySpan<T> ReadOnlySpanImplicitCastFromArraySegment() => _arraySegment;

        [Benchmark]
        public System.ReadOnlySpan<T> ReadOnlySpanImplicitCastFromSpan() => System.Span<T>.Empty;

        [Benchmark]
        public System.Memory<T> MemoryFromArray() => new System.Memory<T>(_nonEmptyArray);

        [Benchmark]
        public System.ReadOnlyMemory<T> ReadOnlyMemoryFromArray() => new System.ReadOnlyMemory<T>(_nonEmptyArray);

        [Benchmark]
        public System.Memory<T> MemoryFromArrayStartLength() => new System.Memory<T>(_nonEmptyArray, start: 0, length: Size);

        [Benchmark]
        public System.ReadOnlyMemory<T> ReadOnlyMemoryFromArrayStartLength() => new System.ReadOnlyMemory<T>(_nonEmptyArray, start: 0, length: Size);

#if NETCOREAPP2_1 // netcoreapp specific API https://github.com/dotnet/coreclr/issues/16126
        [Benchmark]
        public System.Span<T> MemoryMarshalCreateSpan() => MemoryMarshal.CreateSpan<T>(ref _field, Size);
    
        [Benchmark]
        public System.ReadOnlySpan<T> MemoryMarshalCreateReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan<T>(ref _field, Size);
#endif
    }
}