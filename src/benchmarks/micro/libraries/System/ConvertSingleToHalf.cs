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
    public class ConvertSingleToHalf
    {
        private float[] bufferSrc;
        private Half[] bufferDst;

        [GlobalSetup]
        public void Setup()
        {
            const int Samples = 65536;
            var vS = bufferSrc = new float[Samples];
            bufferDst = new Half[Samples];
            Span<float> vspan = vS.AsSpan();
            for (var i = 0; i < vspan.Length; i++)
            {
                vspan[i] = (float)BitConverter.UInt16BitsToHalf((ushort)i);
            }
            //Random Permutation
            ref float x9 = ref MemoryMarshal.GetReference(vspan);
            int length = vspan.Length;
            int olen = length - 2;
            var rng = new Random(12345);    //ValuesGenerator doesn't support exhaustive permutation
            for (var i = 0; i < olen; i++)
            {
                int x = rng.Next(i, length);
                (Unsafe.Add(ref x9, x), Unsafe.Add(ref x9, i)) = (Unsafe.Add(ref x9, i), Unsafe.Add(ref x9, x));
            }
        }

        [Benchmark(Baseline = true)]
        public void SimpleLoop()
        {
            Span<float> bA = bufferSrc.AsSpan();
            Span<Half> bD = bufferDst.AsSpan();
            ref float rsi = ref MemoryMarshal.GetReference(bA);
            ref Half rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (Half)Unsafe.Add(ref rsi, i);
            }
        }

        [Benchmark]
        public void UnrolledLoop()
        {
            Span<float> bA = bufferSrc.AsSpan();
            Span<Half> bD = bufferDst.AsSpan();
            ref float rsi = ref MemoryMarshal.GetReference(bA);
            ref Half rdi = ref MemoryMarshal.GetReference(bD);
            nint i = 0, length = Math.Min(bA.Length, bD.Length);
            var olen = length - 3;
            for (; i < olen; i += 4)
            {
                Unsafe.Add(ref rdi, i + 0) = (Half)Unsafe.Add(ref rsi, i + 0);
                Unsafe.Add(ref rdi, i + 1) = (Half)Unsafe.Add(ref rsi, i + 1);
                Unsafe.Add(ref rdi, i + 2) = (Half)Unsafe.Add(ref rsi, i + 2);
                Unsafe.Add(ref rdi, i + 3) = (Half)Unsafe.Add(ref rsi, i + 3);
            }
            for (; i < length; i++)
            {
                Unsafe.Add(ref rdi, i) = (Half)Unsafe.Add(ref rsi, i);
            }
        }
    }
}
