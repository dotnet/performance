using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using MicroBenchmarks;

namespace SveBenchmarks
{
    [BenchmarkCategory(Categories.Runtime)]
    [OperatingSystemsArchitectureFilter(allowed: true, System.Runtime.InteropServices.Architecture.Arm64)]
    [Config(typeof(Config))]
    public class PairwiseAdd
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddFilter(new SimpleFilter(_ => Sve2.IsSupported));
            }
        }

        [Params(15, 127, 527, 10015)]
        public int Size;

        private int[] _source1;
        private int[] _source2;
        private int[] _result;

        [GlobalSetup]
        public virtual void Setup()
        {
            _source1 = ValuesGenerator.Array<int>(Size * 2);
            _source2 = new int[Size * 2];
            for (int i = 0; i < _source2.Length; i++)
            {
                _source2[i] = _source1[i] * 2 + 3;
            }
            _result = new int[Size * 2];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            int[] current = (int[])_result.Clone();
            Setup();
            Scalar();
            int[] scalar = (int[])_result.Clone();
            // Check that the result is the same as the scalar result.
            for (int i = 0; i < current.Length; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_113.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (int* a = _source1, b = _source2, c = _result)
            {
                for (int i = 0; i < Size * 2; i += 2)
                {
                    c[i] = a[i] + a[i + 1];
                    c[i + 1] = b[i] + b[i + 1];
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128PairwiseAdd()
        {
            fixed (int* a = _source1, b = _source2, c = _result)
            {
                int i = 0;
                int lmt = (Size * 2) - 8;

                for (; i < lmt; i += 8)
                {
                    // Load 2 vectors worth of elements from a and b.
                    Vector128<int> a0 = AdvSimd.LoadVector128(a + i);
                    Vector128<int> b0 = AdvSimd.LoadVector128(b + i);
                    Vector128<int> a1 = AdvSimd.LoadVector128(a + i + 4);
                    Vector128<int> b1 = AdvSimd.LoadVector128(b + i + 4);

                    // Pairwise add the vectors a and b.
                    Vector128<int> c0 = AdvSimd.Arm64.AddPairwise(a0, a1);
                    Vector128<int> c1 = AdvSimd.Arm64.AddPairwise(b0, b1);

                    // Store the results to c.
                    AdvSimd.Arm64.StoreVectorAndZip(c + i, (c0, c1));
                }

                // Handle remaining elements.
                for (; i < Size * 2; i += 2)
                {
                    c[i] = a[i] + a[i + 1];
                    c[i + 1] = b[i] + b[i + 1];
                }
            }
        }

        [Benchmark]
        public unsafe void SvePairwiseAdd()
        {
            fixed (int* a = _source1, b = _source2, c = _result)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();
                // Set limit to Size * 2 - cntw * 2.
                int lmt = (Size - cntw) * 2;

                Vector<int> pTrue = Sve.CreateTrueMaskInt32();
                for (; i <= lmt; i += cntw << 1)
                {
                    // Load and unzip 2 vectors worth of elements.
                    (Vector<int> a0, Vector<int> a1) = Sve.Load2xVectorAndUnzip(pTrue, a + i);
                    (Vector<int> b0, Vector<int> b1) = Sve.Load2xVectorAndUnzip(pTrue, b + i);

                    // Add the components of a and b respectively.
                    Vector<int> c0 = Sve.Add(a0, a1);
                    Vector<int> c1 = Sve.Add(b0, b1);

                    // Interleave store the results to c.
                    Sve.StoreAndZip(pTrue, c + i, (c0, c1));
                }

                // Handle remaining elements using predicates.
                lmt = Size * 2;
                Vector<int> pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, lmt);
                if (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Compute the predicate for elements in i + cntw.
                    Vector<int> pTail = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i + cntw, lmt);
                    // Unzip the predicates pLoop and pTail for 2xVector load/store.
                    Vector<int> pInner = Sve.UnzipEven(pLoop, pTail);

                    (Vector<int> a0, Vector<int> a1) = Sve.Load2xVectorAndUnzip(pInner, a + i);
                    (Vector<int> b0, Vector<int> b1) = Sve.Load2xVectorAndUnzip(pInner, b + i);
                    Vector<int> c0 = Sve.Add(a0, a1);
                    Vector<int> c1 = Sve.Add(b0, b1);
                    Sve.StoreAndZip(pInner, c + i, (c0, c1));
                }
            }
        }

        [Benchmark]
        public unsafe void Sve2PairwiseAdd()
        {
            fixed (int* a = _source1, b = _source2, c = _result)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();
                // Set limit to Size * 2 - cntw * 2.
                int lmt = (Size - cntw) * 2;

                Vector<int> pTrue = Sve.CreateTrueMaskInt32();
                // Unroll loop to handle 2 vectors at a time.
                for  (; i <= lmt; i += cntw << 1)
                {
                    // Load 2 vectors from a and b.
                    Vector<int> a0 = Sve.LoadVector(pTrue, a + i);
                    Vector<int> b0 = Sve.LoadVector(pTrue, b + i);
                    Vector<int> a1 = Sve.LoadVector(pTrue, a + i + cntw);
                    Vector<int> b1 = Sve.LoadVector(pTrue, b + i + cntw);

                    // Pairwise add the vectors a and b.
                    Vector<int> c0 = Sve2.AddPairwise(a0, b0);
                    Vector<int> c1 = Sve2.AddPairwise(a1, b1);

                    // Store the results to c.
                    Sve.StoreAndZip(pTrue, c + i, c0);
                    Sve.StoreAndZip(pTrue, c + i + cntw, c1);
                }

                // Handle remaining elements.
                lmt = Size * 2;
                Vector<int> pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, lmt);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<int> a0 = Sve.LoadVector(pLoop, a + i);
                    Vector<int> b0 = Sve.LoadVector(pLoop, b + i);
                    Vector<int> c0 = Sve2.AddPairwise(a0, b0);
                    Sve.StoreAndZip(pLoop, c + i, c0);
                    i += cntw;
                    pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, lmt);
                }
            }
        }


    }
}
