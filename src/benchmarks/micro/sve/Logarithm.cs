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
    public class Logarithm
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
        private float[] _data;
        private float[] _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            Random rand = new Random(0);
            _input = new float[Size];
            for (int i = 0; i < Size; i++)
            {
                _input[i] = (float)(rand.NextDouble() * (double)Size);
            }

            // Coefficients taken from Arm Optimized-Routines.
            // https://github.com/ARM-software/optimized-routines/blob/v25.07/math/aarch64/advsimd/logf.c
            _data = new float[8]{
                // p0, p1, p3, p5
                BitConverter.UInt32BitsToSingle(0xbe1f39be),
                BitConverter.UInt32BitsToSingle(0x3e2d4d51),
                BitConverter.UInt32BitsToSingle(0x3e4b09a4),
                BitConverter.UInt32BitsToSingle(0x3eaaaebe),
                // p2, p4, p6, ln2
                BitConverter.UInt32BitsToSingle(0xbe27cc9a),
                BitConverter.UInt32BitsToSingle(0xbe800c3e),
                BitConverter.UInt32BitsToSingle(0xbeffffe4),
                BitConverter.UInt32BitsToSingle(0x3f317218),
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
                    output[i] = (float)Math.Log(input[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128Logarithm()
        {
            // Algorithm based on Arm Optimized-Routines.
            // https://github.com/ARM-software/optimized-routines/blob/v25.07/math/aarch64/advsimd/logf.c
            fixed (float* input = _input, output = _output, d = _data)
            {
                int i = 0;
                Vector128<uint> offVec = Vector128.Create(0x3f2aaaabu);

                for (; i <= Size - 4; i += 4)
                {
                    Vector128<float> x = AdvSimd.LoadVector128(input + i);
                    Vector128<uint> u_off = AdvSimd.Subtract(x.AsUInt32(), offVec);

                    Vector64<ushort> cmp = AdvSimd.CompareGreaterThanOrEqual(
                        AdvSimd.SubtractHighNarrowingLower(u_off, Vector128.Create(0xc1555555u)), // u_off - (0x00800000 - 0x3f2aaaab)
                        Vector64.Create((ushort)0x7f00)
                    );

                    // x = 2^n * (1+r), where 2/3 < 1+r < 4/3.
                    Vector128<float> n = AdvSimd.ConvertToSingle(
                        AdvSimd.ShiftRightArithmetic(u_off.AsInt32(), 23)
                    );

                    Vector128<uint> u = AdvSimd.And(u_off, Vector128.Create(0x007fffffu));
                    u = AdvSimd.Add(u, offVec);

                    Vector128<float> r = AdvSimd.Subtract(u.AsSingle(), Vector128.Create(1.0f));
                    // y = log(1+r) + n*ln2.
                    Vector128<float> r2 = AdvSimd.Multiply(r, r);

                    // n*ln2 + r + r2*(P6 + r*P5 + r2*(P4 + r*P3 + r2*(P2 + r*P1 + r2*P0))).
                    Vector128<float> p_0135 = AdvSimd.LoadVector128(&d[0]);
                    Vector128<float> p = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(Vector128.Create(d[4]), r, p_0135, 1);
                    Vector128<float> q = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(Vector128.Create(d[5]), r, p_0135, 2);
                    Vector128<float> y = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(Vector128.Create(d[6]), r, p_0135, 3);
                    p = AdvSimd.Arm64.FusedMultiplyAddBySelectedScalar(p, r2, p_0135, 0);

                    q = AdvSimd.FusedMultiplyAdd(q, r2, p);
                    y = AdvSimd.FusedMultiplyAdd(y, r2, q);
                    p = AdvSimd.FusedMultiplyAdd(r, n, Vector128.Create(d[7]));

                    Vector128<float> outVec = AdvSimd.FusedMultiplyAdd(p, r2, y);

                    // Handle special case.
                    if (cmp.AsUInt64().ToScalar() != 0)
                    {
                        // Restore input x.
                        x = AdvSimd.Add(u_off, offVec).AsSingle();
                        // Widen cmp to 32-bit lanes.
                        Vector128<uint> pCmp = AdvSimd.ZeroExtendWideningLower(cmp);
                        // Use scalar for lanes that are special cases.
                        outVec = Vector128.Create(
                            pCmp[0] != 0 ? (float)Math.Log(x[0]) : outVec[0],
                            pCmp[1] != 0 ? (float)Math.Log(x[1]) : outVec[1],
                            pCmp[2] != 0 ? (float)Math.Log(x[2]) : outVec[2],
                            pCmp[3] != 0 ? (float)Math.Log(x[3]) : outVec[3]
                        );
                    }

                    AdvSimd.Store(output + i, outVec);
                }
                // Handle tail.
                for (; i < Size; i++)
                {
                    output[i] = (float)Math.Log(input[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void SveLogarithm()
        {
            // Algorithm based on Arm Optimized-Routines.
            // https://github.com/ARM-software/optimized-routines/blob/v25.07/math/aarch64/sve/logf.c
            fixed (float* input = _input, output = _output, d = _data)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                Vector<uint> offVec = new Vector<uint>(0x3f2aaaab);

                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();
                Vector<float> pTruef = Sve.CreateTrueMaskSingle();
                Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(0, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<float> x = (Vector<float>)Sve.LoadVector(pLoop, (uint*)(input + i));
                    Vector<uint> u_off = Sve.Subtract((Vector<uint>)x, offVec);

                    // Check for extreme values outside of 0x00800000 and 0x00ffffff.
                    Vector<uint> cmp = Sve.CompareGreaterThanOrEqual(
                        Sve.Subtract(u_off, new Vector<uint>(0xc1555555u)), // u_off - (0x00800000 - 0x3f2aaaab)
                        new Vector<uint>(0x7f000000)
                    );

                    // x = 2^n * (1+r), where 2/3 < 1+r < 4/3.
                    Vector<float> n = Sve.ConvertToSingle(
                        Sve.ShiftRightArithmetic((Vector<int>)u_off, new Vector<uint>(23))
                    );

                    Vector<uint> u = Sve.And(u_off, new Vector<uint>(0x007fffff));
                    u = Sve.Add(u, offVec);

                    Vector<float> r = Sve.Subtract((Vector<float>)u, new Vector<float>(1.0f));

                    // y = log(1+r) + n*ln2.
                    Vector<float> r2 = Sve.Multiply(r, r);

                    // n*ln2 + r + r2*(P6 + r*P5 + r2*(P4 + r*P3 + r2*(P2 + r*P1 + r2*P0))).
                    Vector<float> p_0135 = Sve.LoadVector(pTruef, &d[0]);
                    Vector<float> p = Sve.FusedMultiplyAddBySelectedScalar(new Vector<float>(d[4]), r, p_0135, 1);
                    Vector<float> q = Sve.FusedMultiplyAddBySelectedScalar(new Vector<float>(d[5]), r, p_0135, 2);
                    Vector<float> y = Sve.FusedMultiplyAddBySelectedScalar(new Vector<float>(d[6]), r, p_0135, 3);
                    p = Sve.FusedMultiplyAddBySelectedScalar(p, r2, p_0135, 0);

                    q = Sve.FusedMultiplyAdd(q, r2, p);
                    y = Sve.FusedMultiplyAdd(y, r2, q);
                    p = Sve.FusedMultiplyAdd(r, n, new Vector<float>(d[7]));

                    Vector<float> outVec = Sve.FusedMultiplyAdd(p, r2, y);
                    // Handle special case.
                    if (Sve.TestAnyTrue(pTrue, cmp))
                    {
                        // Restore input x.
                        x = (Vector<float>)Sve.Add(u_off, offVec);
                        // Get the first extreme value.
                        Vector<uint> pElem = Sve.CreateMaskForFirstActiveElement(
                            cmp, Sve.CreateFalseMaskUInt32()
                        );
                        while (Sve.TestAnyTrue(cmp, pElem))
                        {
                            float elem = Sve.ConditionalExtractLastActiveElement(
                                (Vector<float>)pElem, 0, x
                            );
                            // Fallback to scalar for extreme values.
                            elem = (float)Math.Log(elem);
                            Vector<float> y2 = new Vector<float>(elem);
                            // Replace value back to outVec.
                            outVec = Sve.ConditionalSelect((Vector<float>)pElem, y2, outVec);
                            // Get next extreme value.
                            pElem = Sve.CreateMaskForNextActiveElement(cmp, pElem);
                        }
                    }

                    Sve.StoreAndZip(pLoop, (uint*)output + i, (Vector<uint>)outVec);

                    // Handle loop.
                    i += cntw;
                    pLoop = Sve.CreateWhileLessThanMask32Bit(i, Size);
                }
            }
        }

    }
}
