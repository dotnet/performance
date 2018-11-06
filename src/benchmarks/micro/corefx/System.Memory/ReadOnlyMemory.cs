// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Memory
{
    [GenericTypeArguments(typeof(byte))]
    [GenericTypeArguments(typeof(char))]
    [BenchmarkCategory(Categories.CoreCLR, Categories.CoreFX, Categories.Span)]
    public class ReadOnlyMemory<T>
    {
        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private System.ReadOnlyMemory<T> _memory;

        [GlobalSetup]
        public void Setup() => _memory = new System.ReadOnlyMemory<T>(ValuesGenerator.Array<T>(Size));

        [Benchmark]
        public void Pin()
        {
            using (var pinned = _memory.Pin())
            {
                Consume(in pinned);
            }
        }

        [Benchmark] 
        public T[] ToArray() => _memory.ToArray();
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume(in MemoryHandle _) { }
    }
}