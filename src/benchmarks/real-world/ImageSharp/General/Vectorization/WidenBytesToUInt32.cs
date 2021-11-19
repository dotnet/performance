// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

using SixLabors.ImageSharp.Tuples;

namespace SixLabors.ImageSharp.Benchmarks.General.Vectorization
{
    
    public class WidenBytesToUInt32
    {
        private byte[] source;

        private uint[] dest;

        private const int Count = 64;

        [GlobalSetup]
        public void Setup()
        {
            this.source = new byte[Count];
            this.dest = new uint[Count];
        }

        [Benchmark]
        public void Simd()
        {
            int n = Count / Vector<byte>.Count;

            ref Vector<byte> sBase = ref Unsafe.As<byte, Vector<byte>>(ref this.source[0]);
            ref Vector<uint> dBase = ref Unsafe.As<uint, Vector<uint>>(ref this.dest[0]);

            for (int i = 0; i < n; i++)
            {
                Vector<byte> b = Unsafe.Add(ref sBase, i);

                Vector.Widen(b, out Vector<ushort> s0, out Vector<ushort> s1);
                Vector.Widen(s0, out Vector<uint> w0, out Vector<uint> w1);
                Vector.Widen(s1, out Vector<uint> w2, out Vector<uint> w3);

                ref Vector<uint> d = ref Unsafe.Add(ref dBase, i * 4);
                d = w0;
                Unsafe.Add(ref d, 1) = w1;
                Unsafe.Add(ref d, 2) = w2;
                Unsafe.Add(ref d, 3) = w3;
            }
        }
    }
}
