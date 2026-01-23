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
    public class Exponent
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddFilter(new SimpleFilter(_ => Sve.IsSupported));
            }
        }

        [Params(15, 127, 527, 10015)]
        public int Size;

        private float[] _input;
        private float[] _data_sve;
        private float[] _data_neon;
        private float[] _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input = ValuesGenerator.Array<float>(Size);

            _data_sve = new float[] {
                // c1, c3, inv_ln2
                BitConverter.UInt32BitsToSingle(0x3f000000),
                BitConverter.UInt32BitsToSingle(0x3d2aaab5),
                BitConverter.UInt32BitsToSingle(0x3fb8aa3b),
                // ln2_lo, c0, c2, c4
                BitConverter.UInt32BitsToSingle(0x35bfbe8e),
                BitConverter.UInt32BitsToSingle(0x3f800000),
                BitConverter.UInt32BitsToSingle(0x3e2aaaab),
                BitConverter.UInt32BitsToSingle(0x3c057330),
                // ln2_hi, shift
                BitConverter.UInt32BitsToSingle(0x3f317200),
                BitConverter.UInt32BitsToSingle(0x48401fc0),
            };

            _data_neon = new float[] {
                // inv_ln2, ln2_lo, c0, c2
                BitConverter.UInt32BitsToSingle(0x3fb8aa3b),
                BitConverter.UInt32BitsToSingle(0x35bfbe8e),
                BitConverter.UInt32BitsToSingle(0x3c07cfce),
                BitConverter.UInt32BitsToSingle(0x3e2aad40),
                // ln2_hi, shift, c1, c3, c4
                BitConverter.UInt32BitsToSingle(0x3f317200),
                BitConverter.UInt32BitsToSingle(0x4b40007f),
                BitConverter.UInt32BitsToSingle(0x3d2b9d0d),
                BitConverter.UInt32BitsToSingle(0x3efffee3),
                BitConverter.UInt32BitsToSingle(0x3f7ffffb),
            };

            _output = new float[Size];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            float[] current = (float[])_output.Clone();
            Setup();
            Scalar();
            float[] scalar = (float[])_output.Clone();

            // Check that the result is the same as scalar (within 3ULP).
            for (int i = 0; i < Size; i++)
            {
                int e = (int)(BitConverter.SingleToUInt32Bits(scalar[i]) >> 23 & 0xff);
                if (e == 0) e++;
                float ulpScale = (float)Math.ScaleB(1.0, e - 127 - 23);
                float ulpError = (float)Math.Abs(current[i] - scalar[i]) / ulpScale;
                Debug.Assert(ulpError <= 3);
            }
        }

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (float* input = _input, output = _output)
            {
                for (int i = 0; i < Size; i++)
                {
                    output[i] = (float)Math.Exp(input[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128Exponent()
        {
            // Algorithm based on Arm Optimized-Routines.
            // https://github.com/ARM-software/optimized-routines/blob/v25.07/math/aarch64/advsimd/expf.c
            fixed (float* input = _input, output = _output, d = _data_neon)
            {
                int i = 0;

                Vector128<float> constVec = AdvSimd.LoadVector128(d);
                Vector128<float> ln2hiVec = Vector128.Create(d[4]);
                Vector128<float> shiftVec = Vector128.Create(d[5]);
                Vector128<float> c1Vec = Vector128.Create(d[6]);
                Vector128<float> c3Vec = Vector128.Create(d[7]);
                Vector128<float> c4Vec = Vector128.Create(d[8]);

                for (; i < Size - 4; i += 4)
                {
                    Vector128<float> x = AdvSimd.LoadVector128(input + i);

                    // z = shift + x * 1/ln2
                    Vector128<float> z = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(shiftVec, x, constVec, 0);
                    // -n = shift - z
                    Vector128<float> neg_n = AdvSimd.Subtract(shiftVec, z);
                    // scale = z << 23
                    Vector128<float> scale = AdvSimd.ShiftLeftLogical(z.AsUInt32(), 23).AsSingle();

                    // r = x - n * ln2_hi
                    Vector128<float> r = AdvSimd.FusedMultiplyAdd(x, neg_n, ln2hiVec);
                    // r = r - n * ln2_lo
                    r = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(r, neg_n, constVec, 1);
                    Vector128<float> r2 = AdvSimd.Multiply(r, r);

                    // poly(r) = exp(r) - 1.
                    Vector128<float> p10 = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(c1Vec, r, constVec, 2);
                    Vector128<float> p32 = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(c3Vec, r, constVec, 3);
                    Vector128<float> p30 = AdvSimd.FusedMultiplyAdd(p32, r2, p10);
                    Vector128<float> p4 = AdvSimd.Multiply(r, c4Vec);
                    Vector128<float> poly = AdvSimd.FusedMultiplyAdd(p4, r2, p30);

                    // result = scale * (1 + poly).
                    Vector128<float> result = AdvSimd.FusedMultiplyAdd(scale, poly, scale);
                    AdvSimd.Store(output + i, result);
                }
                // Handle remaining elements.
                for (; i < Size; i++)
                {
                    output[i] = (float)Math.Exp(input[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void SveExponent()
        {
            fixed (float* input = _input, output = _output, d = _data_sve)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                Vector<float> c1Vec = new Vector<float>(d[0]);
                Vector<float> c3Vec = new Vector<float>(d[1]);
                Vector<float> invln2Vec = new Vector<float>(d[2]);
                Vector<float> shiftVec = new Vector<float>(d[8]);
                Vector<float> ln2hiVec = new Vector<float>(d[7]);
                Vector<float> constVec = Sve.LoadVector(Sve.CreateTrueMaskSingle(), &d[3]);

                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();
                Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(0, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<float> x = (Vector<float>)Sve.LoadVector(pLoop, (uint*)(input + i));

                    // n = round(x/(ln2/N)).
                    Vector<float> z = Sve.FusedMultiplyAdd(shiftVec, invln2Vec, x);
                    Vector<float> n = Sve.Subtract(z, shiftVec);

                    // r = x - n*ln2/N.
                    Vector<float> r = Sve.FusedMultiplySubtract(x, ln2hiVec, n);
                    r = Sve.FusedMultiplySubtractBySelectedScalar(r, n, constVec, 0);
                    // scale = 2^(n/N).
                    Vector<float> scale = Sve.FloatingPointExponentialAccelerator((Vector<uint>)z);

                    // poly(r) = exp(r) - 1.
                    Vector<float> p12 = Sve.FusedMultiplyAddBySelectedScalar(c1Vec, r, constVec, 2);
                    Vector<float> p34 = Sve.FusedMultiplyAddBySelectedScalar(c3Vec, r, constVec, 3);
                    Vector<float> r2 = Sve.Multiply(r, r);
                    Vector<float> p14 = Sve.FusedMultiplyAdd(p12, p34, r2);
                    Vector<float> p0 = Sve.MultiplyBySelectedScalar(r, constVec, 1);
                    Vector<float> poly = Sve.FusedMultiplyAdd(p0, r2, p14);

                    // result = scale * (1 + poly).
                    Vector<float> result = Sve.FusedMultiplyAdd(scale, poly, scale);
                    Sve.StoreAndZip(pLoop, (uint*)output + i, (Vector<uint>)result);

                    // Handle loop.
                    i += cntw;
                    pLoop = Sve.CreateWhileLessThanMask32Bit(i, Size);
                }
            }
        }

    }
}
