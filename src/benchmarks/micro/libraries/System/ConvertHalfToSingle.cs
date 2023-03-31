using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using MicroBenchmarks;

namespace System
{
    [BenchmarkCategory(Categories.Runtime, Categories.Libraries)]
    public class ConvertHalfToSingle
    {
        private Half[] bufferA;
        private float[] bufferDst;


        [GlobalSetup]
        public void Setup()
        {
            var samples = 65536;
            bufferDst = new float[samples];
            var bA = bufferA = new Half[samples];
            var spanA = bA.AsSpan();
            for (var i = 0; i < spanA.Length; i++)
            {
                spanA[i] = BitConverter.UInt16BitsToHalf((ushort)i);
            }
            ref var x9 = ref MemoryMarshal.GetReference(spanA);
            var length = spanA.Length;
            var olen = length - 2;
            var rng = new Random(12345);    //ValuesGenerator doesn't support exhaustive permutation
            for (var i = 0; i < olen; i++)
            {
                var x = rng.Next(i, length);
                (Unsafe.Add(ref x9, x), Unsafe.Add(ref x9, i)) = (Unsafe.Add(ref x9, i), Unsafe.Add(ref x9, x));
            }
        }

        [Benchmark]
        public void SimpleLoop()
        {
            var bA = bufferA.AsSpan();
            var bD = bufferDst.AsSpan();
            ref var rsi = ref MemoryMarshal.GetReference(bA);
            ref var rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (float)Unsafe.Add(ref rsi, i);
            }
        }

        [Benchmark]
        public void UnrolledLoop()
        {
            var bA = bufferA.AsSpan();
            var bD = bufferDst.AsSpan();
            ref var rsi = ref MemoryMarshal.GetReference(bA);
            ref var rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            var olen = length - 3;
            for (; i < olen; i += 4)
            {
                Unsafe.Add(ref rdi, i + 0) = (float)Unsafe.Add(ref rsi, i + 0);
                Unsafe.Add(ref rdi, i + 1) = (float)Unsafe.Add(ref rsi, i + 1);
                Unsafe.Add(ref rdi, i + 2) = (float)Unsafe.Add(ref rsi, i + 2);
                Unsafe.Add(ref rdi, i + 3) = (float)Unsafe.Add(ref rsi, i + 3);
            }
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (float)Unsafe.Add(ref rsi, i);
            }
        }
    }
}
