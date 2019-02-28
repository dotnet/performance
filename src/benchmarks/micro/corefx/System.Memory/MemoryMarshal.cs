// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(int))]
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class MemoryMarshal<T>
        where T : struct
    {
        private T[] _array;
        private System.ReadOnlyMemory<T> _memory;

        public MemoryMarshal()
        {
            _array = ValuesGenerator.Array<T>(Utils.DefaultCollectionSize);
            _memory = new System.ReadOnlyMemory<T>(_array);
        }

        [Benchmark]
        public ref T GetReference() => ref MemoryMarshal.GetReference(new System.Span<T>(_array));
        
        [Benchmark]
        public System.Span<byte> AsBytes() => MemoryMarshal.AsBytes(new System.Span<T>(_array));

        [Benchmark]
        public System.Span<byte> CastToByte() => MemoryMarshal.Cast<T, byte>(new System.Span<T>(_array));
        
        [Benchmark]
        public System.Span<int> CastToInt() => MemoryMarshal.Cast<T, int>(new System.Span<T>(_array));
        
        [Benchmark]
        public bool TryGetArray() => MemoryMarshal.TryGetArray(_memory, out var _);

        [Benchmark(OperationsPerInvoke = 16)]
        public void Read()
        {
            System.ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(new System.ReadOnlySpan<T>(_array));
            
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
            Consume(MemoryMarshal.Read<TestStructExplicit>(bytes)); Consume(MemoryMarshal.Read<TestStructExplicit>(bytes));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Consume(in TestStructExplicit _) { }
    }
    
    [StructLayout(LayoutKind.Explicit)]
    internal struct TestStructExplicit
    {
        [FieldOffset(0)]
        public short S0;
        [FieldOffset(2)]
        public int I0;
        [FieldOffset(6)]
        public long L0;
        [FieldOffset(14)]
        public ushort US0;
        [FieldOffset(16)]
        public uint UI0;
        [FieldOffset(20)]
        public ulong UL0;
        [FieldOffset(28)]
        public short S1;
        [FieldOffset(30)]
        public int I1;
        [FieldOffset(34)]
        public long L1;
        [FieldOffset(42)]
        public ushort US1;
        [FieldOffset(44)]
        public uint UI1;
        [FieldOffset(48)]
        public ulong UL1;
    }
}