// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            System.Span<T> span = new System.Span<T>(_nonEmptyArray);

            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));

            return span;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.Span<T> SpanStartLength()
        {
            System.Span<T> span = new System.Span<T>(_nonEmptyArray);

            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));

            return span;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlySpan<T> ReadOnlySpanStart()
        {
            System.ReadOnlySpan<T> span = new System.ReadOnlySpan<T>(_nonEmptyArray);
            
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));
            Consume(span.Slice(Size / 2)); Consume(span.Slice(Size / 2));

            return span;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlySpan<T> ReadOnlySpanStartLength()
        {
            System.ReadOnlySpan<T> span = new System.ReadOnlySpan<T>(_nonEmptyArray);
            
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));
            Consume(span.Slice(Size / 2, 1)); Consume(span.Slice(Size / 2, 1));

            return span;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.Memory<T> MemoryStart()
        {
            System.Memory<T> memory = new System.Memory<T>(_nonEmptyArray);

            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));

            return memory;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.Memory<T> MemoryStartLength()
        {
            System.Memory<T> memory = new System.Memory<T>(_nonEmptyArray);

            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));

            return memory;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlyMemory<T> ReadOnlyMemoryStart()
        {
            System.ReadOnlyMemory<T> memory = new System.ReadOnlyMemory<T>(_nonEmptyArray);

            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));
            Consume(memory.Slice(Size / 2)); Consume(memory.Slice(Size / 2));

            return memory;
        }
        
        [Benchmark(OperationsPerInvoke = 16)]
        public System.ReadOnlyMemory<T> ReadOnlyMemoryStartLength()
        {
            System.ReadOnlyMemory<T> memory = new System.ReadOnlyMemory<T>(_nonEmptyArray);

            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));
            Consume(memory.Slice(Size / 2, 1)); Consume(memory.Slice(Size / 2, 1));

            return memory;
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