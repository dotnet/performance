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
    public class ComplexMultiply
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

        private uint[] _source1;
        private uint[] _source2;
        private uint[] _result;

        [GlobalSetup]
        public virtual void Setup()
        {
            _source1 = ValuesGenerator.Array<uint>(Size * 2);
            _source2 = ValuesGenerator.Array<uint>(Size * 2);
            _result  = new uint[Size * 2];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            uint[] current = (uint[])_result.Clone();
            Setup();
            Scalar();
            uint[] scalar = (uint[])_result.Clone();
            // Check that the result is the same as the scalar result.
            for (int i = 0; i < current.Length; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_112.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (uint* a = _source1, b = _source2, c = _result)
            {
                for (int i = 0; i < Size * 2; i += 2)
                {
                    // Index i is the real part, i + 1 is the imaginary part.
                    c[i] = (a[i] * b[i]) - (a[i + 1] * b[i + 1]);
                    c[i + 1] = (a[i] * b[i + 1]) + (a[i + 1] * b[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128ComplexMultiply()
        {
            fixed (uint* a = _source1, b = _source2, c = _result)
            {
                int i = 0;
                int lmt = (Size * 2) - 8;
                for (; i <= lmt; i += 8)
                {
                    Vector128<uint> cRe = Vector128<uint>.Zero;
                    Vector128<uint> cIm = Vector128<uint>.Zero;

                    // Load real and imaginary parts separately.
                    (Vector128<uint> aRe, Vector128<uint> aIm) = AdvSimd.Arm64.Load2xVector128AndUnzip(a + i);
                    (Vector128<uint> bRe, Vector128<uint> bIm) = AdvSimd.Arm64.Load2xVector128AndUnzip(b + i);

                    // Perform multiplication.
                    cRe = AdvSimd.MultiplyAdd(cRe, aRe, bRe);
                    cRe = AdvSimd.MultiplySubtract(cRe, aIm, bIm);
                    cIm = AdvSimd.MultiplyAdd(cIm, aRe, bIm);
                    cIm = AdvSimd.MultiplyAdd(cIm, aIm, bRe);

                    // Store the output real and imaginary parts.
                    AdvSimd.Arm64.StoreVectorAndZip(c + i, (cRe, cIm));
                }
                for (; i < Size * 2; i += 2)
                {
                    c[i] = (a[i] * b[i]) - (a[i + 1] * b[i + 1]);
                    c[i + 1] = (a[i] * b[i + 1]) + (a[i + 1] * b[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void SveComplexMultiply()
        {
            fixed (uint* a = _source1, b = _source2, c = _result)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();
                // Set limit to Size * 2 - cntw * 2.
                int lmt = (Size - cntw) * 2;

                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();
                for (; i <= lmt; i += (cntw << 1))
                {
                    Vector<uint> cRe = Vector<uint>.Zero;
                    Vector<uint> cIm = Vector<uint>.Zero;

                    // Load real and imaginary parts separately.
                    (Vector<uint> aRe, Vector<uint> aIm) = Sve.Load2xVectorAndUnzip(pTrue, a + i);
                    (Vector<uint> bRe, Vector<uint> bIm) = Sve.Load2xVectorAndUnzip(pTrue, b + i);

                    // Perform multiplication.
                    cRe = Sve.MultiplyAdd(cRe, aRe, bRe);
                    cRe = Sve.MultiplySubtract(cRe, aIm, bIm);
                    cIm = Sve.MultiplyAdd(cIm, aRe, bIm);
                    cIm = Sve.MultiplyAdd(cIm, aIm, bRe);

                    // Interleaved store the output real and imaginary parts.
                    Sve.StoreAndZip(pTrue, c + i, (cRe, cIm));
                }

                // Handle remaining elements using predicates.
                lmt = Size * 2;
                Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(i, lmt);
                if (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Compute the predicate for elements in i + cntw.
                    Vector<uint> pTail = Sve.CreateWhileLessThanMask32Bit(i + cntw, lmt);
                    // Unzip the predicates pLoop and pTail for 2xVector load/store.
                    Vector<uint> pInner = Sve.UnzipEven(pLoop, pTail);

                    Vector<uint> cRe = Vector<uint>.Zero;
                    Vector<uint> cIm = Vector<uint>.Zero;
                    (Vector<uint> aRe, Vector<uint> aIm) = Sve.Load2xVectorAndUnzip(pInner, a + i);
                    (Vector<uint> bRe, Vector<uint> bIm) = Sve.Load2xVectorAndUnzip(pInner, b + i);
                    cRe = Sve.MultiplyAdd(cRe, aRe, bRe);
                    cRe = Sve.MultiplySubtract(cRe, aIm, bIm);
                    cIm = Sve.MultiplyAdd(cIm, aRe, bIm);
                    cIm = Sve.MultiplyAdd(cIm, aIm, bRe);
                    Sve.StoreAndZip(pInner, c + i, (cRe, cIm));
                }
            }
        }

        [Benchmark]
        public unsafe void Sve2ComplexMultiply()
        {
            fixed (uint* a = _source1, b = _source2, c = _result)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();
                int lmt = Size - cntd;

                Vector<ulong> pTrue = Sve.CreateTrueMaskUInt64();
                for (; i <= lmt; i += (cntd << 1))
                {
                    Vector<uint> c1 = Vector<uint>.Zero;
                    Vector<uint> c2 = Vector<uint>.Zero;

                    // Read complex numbers as 64-bit then reinterpret as 32-bit vectors.
                    Vector<uint> a1 = (Vector<uint>)Sve2.LoadVector(pTrue, (ulong*)a + i);
                    Vector<uint> a2 = (Vector<uint>)Sve2.LoadVector(pTrue, (ulong*)a + i + cntd);
                    Vector<uint> b1 = (Vector<uint>)Sve2.LoadVector(pTrue, (ulong*)b + i);
                    Vector<uint> b2 = (Vector<uint>)Sve2.LoadVector(pTrue, (ulong*)b + i + cntd);

                    // Perform multiplication.
                    c1 = Sve2.MultiplyAddRotateComplex(c1, a1, b1, 0);
                    c1 = Sve2.MultiplyAddRotateComplex(c1, a1, b1, 1);
                    c2 = Sve2.MultiplyAddRotateComplex(c2, a2, b2, 0);
                    c2 = Sve2.MultiplyAddRotateComplex(c2, a2, b2, 1);

                    // Store to output as 64-bit vectors.
                    Sve2.StoreAndZip(pTrue, (ulong*)c + i, (Vector<ulong>)(c1));
                    Sve2.StoreAndZip(pTrue, (ulong*)c + i + cntd, (Vector<ulong>)(c2));
                }

                // Handle remaining elements.
                lmt = Size;
                Vector<ulong> pLoop = Sve.CreateWhileLessThanMask64Bit(i, lmt);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<uint> a1 = (Vector<uint>)Sve2.LoadVector(pLoop, (ulong*)a + i);
                    Vector<uint> b1 = (Vector<uint>)Sve2.LoadVector(pLoop, (ulong*)b + i);
                    Vector<uint> c1 = Vector<uint>.Zero;
                    c1 = Sve2.MultiplyAddRotateComplex(c1, a1, b1, 0);
                    c1 = Sve2.MultiplyAddRotateComplex(c1, a1, b1, 1);
                    Sve.StoreAndZip(pLoop, (ulong*)c + i, (Vector<ulong>)(c1));

                    i += cntd;
                    pLoop = Sve.CreateWhileLessThanMask64Bit(i, lmt);
                }
            }
        }

    }
}
