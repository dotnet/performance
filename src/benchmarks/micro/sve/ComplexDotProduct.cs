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
    public class ComplexDotProduct
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

        private sbyte[] _input1;
        private sbyte[] _input2;
        private int[] _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input1 = new sbyte[Size * 4];
            _input2 = new sbyte[Size * 4];

            sbyte[] vals = ValuesGenerator.Array<sbyte>(Size * 8);
            for (int i = 0; i < Size * 4; i++)
            {
                _input1[i] = vals[i];
                _input2[i] = vals[Size * 4 + i];
            }

            _output = new int[Size * 2];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            int[] current = (int[])_output.Clone();
            Setup();
            Scalar();
            int[] scalar = (int[])_output.Clone();

            // Check that the result is the same as scalar.
            for (int i = 0; i < Size; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_110.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (sbyte* a = _input1, b = _input2)
            fixed (int* c = _output)
            {
                for (int i = 0; i < Size; i++)
                {
                    // Real part.
                    c[i * 2] = (int)(((a[4 * i] * b[4 * i]) - (a[4 * i + 1] * b[4 * i + 1])) +
                                     ((a[4 * i + 2] * b[4 * i + 2]) - (a[4 * i + 3] * b[4 * i + 3])));
                    // Imaginary part.
                    c[i * 2 + 1] = (int)(((a[4 * i + 1] * b[4 * i]) + (a[4 * i] * b[4 * i + 1])) +
                                         ((a[4 * i + 3] * b[4 * i + 2]) + (a[4 * i + 2] * b[4 * i + 3])));
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128ComplexDotProduct()
        {
            fixed (sbyte* a = _input1, b = _input2)
            fixed (int* c = _output)
            {

                // Helper function for calculating dot product.
                Func<Vector128<int>, Vector128<int>, Vector128<int>, Vector128<int>,
                     Vector128<int>, Vector128<int>, Vector128<int>, Vector128<int>,
                     (Vector128<int>, Vector128<int>)> dotProduct = (a0, a1, a2, a3, b0, b1, b2, b3) => {

                    Vector128<int> cr = Vector128<int>.Zero;
                    Vector128<int> ci = Vector128<int>.Zero;

                    // Compute dot product of the real part.
                    cr = AdvSimd.MultiplyAdd(cr, a0, b0);
                    cr = AdvSimd.MultiplySubtract(cr, a1, b1);
                    cr = AdvSimd.MultiplyAdd(cr, a2, b2);
                    cr = AdvSimd.MultiplySubtract(cr, a3, b3);
                    // Compute dot product of the imaginary part.
                    ci = AdvSimd.MultiplyAdd(ci, a0, b1);
                    ci = AdvSimd.MultiplyAdd(ci, a1, b0);
                    ci = AdvSimd.MultiplyAdd(ci, a2, b3);
                    ci = AdvSimd.MultiplyAdd(ci, a3, b2);

                    return (cr, ci);
                };

                int i = 0;
                int lmt = Size - 16;
                for (; i <= lmt; i += 16)
                {
                    // Load inputs as sbytes.
                    (Vector128<sbyte> a0, Vector128<sbyte> a1, Vector128<sbyte> a2, Vector128<sbyte> a3) = AdvSimd.Arm64.Load4xVector128AndUnzip(a + 4 * i);
                    (Vector128<sbyte> b0, Vector128<sbyte> b1, Vector128<sbyte> b2, Vector128<sbyte> b3) = AdvSimd.Arm64.Load4xVector128AndUnzip(b + 4 * i);
                    Vector128<int> cr, ci;

                    // Extend sbyte to short for each input component.
                    Vector128<short> a0l = AdvSimd.SignExtendWideningLower(a0.GetLower());
                    Vector128<short> a0h = AdvSimd.SignExtendWideningUpper(a0);
                    Vector128<short> a1l = AdvSimd.SignExtendWideningLower(a1.GetLower());
                    Vector128<short> a1h = AdvSimd.SignExtendWideningUpper(a1);
                    Vector128<short> a2l = AdvSimd.SignExtendWideningLower(a2.GetLower());
                    Vector128<short> a2h = AdvSimd.SignExtendWideningUpper(a2);
                    Vector128<short> a3l = AdvSimd.SignExtendWideningLower(a3.GetLower());
                    Vector128<short> a3h = AdvSimd.SignExtendWideningUpper(a3);
                    Vector128<short> b0l = AdvSimd.SignExtendWideningLower(b0.GetLower());
                    Vector128<short> b0h = AdvSimd.SignExtendWideningUpper(b0);
                    Vector128<short> b1l = AdvSimd.SignExtendWideningLower(b1.GetLower());
                    Vector128<short> b1h = AdvSimd.SignExtendWideningUpper(b1);
                    Vector128<short> b2l = AdvSimd.SignExtendWideningLower(b2.GetLower());
                    Vector128<short> b2h = AdvSimd.SignExtendWideningUpper(b2);
                    Vector128<short> b3l = AdvSimd.SignExtendWideningLower(b3.GetLower());
                    Vector128<short> b3h = AdvSimd.SignExtendWideningUpper(b3);

                    // Compute the lower half of the lower half.
                    Vector128<int> a0ll = AdvSimd.SignExtendWideningLower(a0l.GetLower());
                    Vector128<int> a1ll = AdvSimd.SignExtendWideningLower(a1l.GetLower());
                    Vector128<int> a2ll = AdvSimd.SignExtendWideningLower(a2l.GetLower());
                    Vector128<int> a3ll = AdvSimd.SignExtendWideningLower(a3l.GetLower());
                    Vector128<int> b0ll = AdvSimd.SignExtendWideningLower(b0l.GetLower());
                    Vector128<int> b1ll = AdvSimd.SignExtendWideningLower(b1l.GetLower());
                    Vector128<int> b2ll = AdvSimd.SignExtendWideningLower(b2l.GetLower());
                    Vector128<int> b3ll = AdvSimd.SignExtendWideningLower(b3l.GetLower());
                    (cr, ci) = dotProduct(a0ll, a1ll, a2ll, a3ll, b0ll, b1ll, b2ll, b3ll);
                    AdvSimd.Arm64.StoreVectorAndZip(c + 2 * i, (cr, ci));

                    // Compute the upper half of the lower half.
                    Vector128<int> a0lh = AdvSimd.SignExtendWideningUpper(a0l);
                    Vector128<int> a1lh = AdvSimd.SignExtendWideningUpper(a1l);
                    Vector128<int> a2lh = AdvSimd.SignExtendWideningUpper(a2l);
                    Vector128<int> a3lh = AdvSimd.SignExtendWideningUpper(a3l);
                    Vector128<int> b0lh = AdvSimd.SignExtendWideningUpper(b0l);
                    Vector128<int> b1lh = AdvSimd.SignExtendWideningUpper(b1l);
                    Vector128<int> b2lh = AdvSimd.SignExtendWideningUpper(b2l);
                    Vector128<int> b3lh = AdvSimd.SignExtendWideningUpper(b3l);
                    (cr, ci) = dotProduct(a0lh, a1lh, a2lh, a3lh, b0lh, b1lh, b2lh, b3lh);
                    AdvSimd.Arm64.StoreVectorAndZip(c + 2 * i + 8, (cr, ci));

                    // Compute the lower half of the upper half.
                    Vector128<int> a0hl = AdvSimd.SignExtendWideningLower(a0h.GetLower());
                    Vector128<int> a1hl = AdvSimd.SignExtendWideningLower(a1h.GetLower());
                    Vector128<int> a2hl = AdvSimd.SignExtendWideningLower(a2h.GetLower());
                    Vector128<int> a3hl = AdvSimd.SignExtendWideningLower(a3h.GetLower());
                    Vector128<int> b0hl = AdvSimd.SignExtendWideningLower(b0h.GetLower());
                    Vector128<int> b1hl = AdvSimd.SignExtendWideningLower(b1h.GetLower());
                    Vector128<int> b2hl = AdvSimd.SignExtendWideningLower(b2h.GetLower());
                    Vector128<int> b3hl = AdvSimd.SignExtendWideningLower(b3h.GetLower());
                    (cr, ci) = dotProduct(a0hl, a1hl, a2hl, a3hl, b0hl, b1hl, b2hl, b3hl);
                    AdvSimd.Arm64.StoreVectorAndZip(c + 2 * i + 16, (cr, ci));

                    // Compute the upper half of the upper half.
                    Vector128<int> a0hh = AdvSimd.SignExtendWideningUpper(a0h);
                    Vector128<int> a1hh = AdvSimd.SignExtendWideningUpper(a1h);
                    Vector128<int> a2hh = AdvSimd.SignExtendWideningUpper(a2h);
                    Vector128<int> a3hh = AdvSimd.SignExtendWideningUpper(a3h);
                    Vector128<int> b0hh = AdvSimd.SignExtendWideningUpper(b0h);
                    Vector128<int> b1hh = AdvSimd.SignExtendWideningUpper(b1h);
                    Vector128<int> b2hh = AdvSimd.SignExtendWideningUpper(b2h);
                    Vector128<int> b3hh = AdvSimd.SignExtendWideningUpper(b3h);
                    (cr, ci) = dotProduct(a0hh, a1hh, a2hh, a3hh, b0hh, b1hh, b2hh, b3hh);
                    AdvSimd.Arm64.StoreVectorAndZip(c + 2 * i + 24, (cr, ci));

                }
                // Handle remaining elements.
                for (; i < Size; i++)
                {
                    // Real part.
                    c[i * 2] = (int)(((a[4 * i] * b[4 * i]) - (a[4 * i + 1] * b[4 * i + 1])) +
                                     ((a[4 * i + 2] * b[4 * i + 2]) - (a[4 * i + 3] * b[4 * i + 3])));
                    // Imaginary part.
                    c[i * 2 + 1] = (int)(((a[4 * i + 1] * b[4 * i]) + (a[4 * i] * b[4 * i + 1])) +
                                         ((a[4 * i + 3] * b[4 * i + 2]) + (a[4 * i + 2] * b[4 * i + 3])));
                }
            }
        }

        [Benchmark]
        public unsafe void SveComplexDotProduct()
        {
            fixed (sbyte* a = _input1, b = _input2)
            fixed (int* c = _output)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                // Create mask for the imaginary half of a word.
                Vector<short> imMask = (Vector<short>)(new Vector<uint>(0xFFFF0000u));
                Vector<int> pTrue = Sve.CreateTrueMaskInt32();
                Vector<int> pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(0, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Load inputs.
                    Vector<sbyte> a1 = (Vector<sbyte>)Sve.LoadVector(pLoop, (int*)(a + 4 * i));
                    Vector<sbyte> b1 = (Vector<sbyte>)Sve.LoadVector(pLoop, (int*)(b + 4 * i));

                    // Unpack and extend the lower and upper halves to short.
                    Vector<short> a1l = Sve.SignExtendWideningLower(a1);
                    Vector<short> a1h = Sve.SignExtendWideningUpper(a1);
                    Vector<short> b1l = Sve.SignExtendWideningLower(b1);
                    Vector<short> b1h = Sve.SignExtendWideningUpper(b1);

                    // Negate the imaginary components.
                    Vector<short> b1l_re = Sve.ConditionalSelect(imMask, Sve.Negate(b1l), b1l);
                    Vector<short> b1h_re = Sve.ConditionalSelect(imMask, Sve.Negate(b1h), b1h);
                    // Swap the real and imaginary components from each word.
                    Vector<short> b1l_im = (Vector<short>)Sve.ReverseElement16((Vector<int>)b1l);
                    Vector<short> b1h_im = (Vector<short>)Sve.ReverseElement16((Vector<int>)b1h);

                    // Compute dot products (real and imaginary, low and high bits).
                    Vector<long> c1l_re = Sve.DotProduct(Vector<long>.Zero, a1l, b1l_re);
                    Vector<long> c1h_re = Sve.DotProduct(Vector<long>.Zero, a1h, b1h_re);
                    Vector<long> c1l_im = Sve.DotProduct(Vector<long>.Zero, a1l, b1l_im);
                    Vector<long> c1h_im = Sve.DotProduct(Vector<long>.Zero, a1h, b1h_im);

                    // Combine low and high parts back together.
                    Vector<int> cr = Sve.UnzipEven((Vector<int>)c1l_re, (Vector<int>)c1h_re);
                    Vector<int> ci = Sve.UnzipEven((Vector<int>)c1l_im, (Vector<int>)c1h_im);

                    // Interleaved store real and imaginary parts of the result.
                    Sve.StoreAndZip(pLoop, c + 2 * i, (cr, ci));

                    // Handle loop.
                    i += cntw;
                    pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, Size);
                }
            }
        }

        [Benchmark]
        public unsafe void Sve2ComplexDotProduct()
        {
            fixed (sbyte* a = _input1, b = _input2)
            fixed (int* c = _output)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                Vector<int> pTrue = Sve.CreateTrueMaskInt32();
                Vector<int> pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(0, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<sbyte> a1 = (Vector<sbyte>)Sve.LoadVector(pLoop, (int*)(a + 4 * i));
                    Vector<sbyte> b1 = (Vector<sbyte>)Sve.LoadVector(pLoop, (int*)(b + 4 * i));

                    Vector<int> cr = Sve2.DotProductRotateComplex(Vector<int>.Zero, a1, b1, 0);
                    Vector<int> ci = Sve2.DotProductRotateComplex(Vector<int>.Zero, a1, b1, 1);

                    Sve.StoreAndZip(pLoop, c + 2 * i, (cr, ci));

                    // Handle loop.
                    i += cntw;
                    pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, Size);
                }
            }
        }

    }
}
