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
    public class UpscaleFilter
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

        private byte[] _input;
        private byte[] _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input = new byte[Size];
            for (int i = 0; i < Size; i++)
            {
                _input[i] = (byte)(i * 3);
            }

            _output = new byte[Size * 2];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            byte[] current = (byte[])_output.Clone();
            Setup();
            Scalar();
            byte[] scalar = (byte[])_output.Clone();
            // Check that the result is the same as the scalar result.
            for (int i = 0; i < current.Length; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_101.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (byte* input = _input, output = _output)
            {
                for (int i = 0; i < Size - 1; i++)
                {
                    ushort s1 = (ushort)input[i];
                    ushort s2 = (ushort)input[i + 1];
                    output[2 * i] = (byte)((3 * s1 + s2 + 2) >> 2);
                    output[2 * i + 1] = (byte)((3 * s2 + s1 + 2) >> 2);
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128UpscaleFilter()
        {
            Vector128<byte> three = Vector128.Create((byte)3);

            fixed (byte* input = _input, output = _output)
            {
                int i = 0;
                int lmt = Size - 1;
                lmt -= lmt % 16;

                for (; i < lmt; i += 16)
                {
                    // Load two consecutive samples.
                    Vector128<byte> b0 = AdvSimd.LoadVector128(input + i);
                    Vector128<byte> b1 = AdvSimd.LoadVector128(input + i + 1);

                    // Initialise accumulators.
                    Vector128<ushort> s0_low = AdvSimd.ZeroExtendWideningLower(b1.GetLower());
                    Vector128<ushort> s0_up = AdvSimd.ZeroExtendWideningUpper(b1);
                    Vector128<ushort> s1_low = AdvSimd.ZeroExtendWideningLower(b0.GetLower());
                    Vector128<ushort> s1_up = AdvSimd.ZeroExtendWideningUpper(b0);

                    // Widened multiply by three and add to result (lower and upper).
                    s0_low = AdvSimd.MultiplyWideningLowerAndAdd(s0_low, b0.GetLower(), three.GetLower());
                    s0_up = AdvSimd.MultiplyWideningUpperAndAdd(s0_up, b0, three);
                    s1_low = AdvSimd.MultiplyWideningLowerAndAdd(s1_low, b1.GetLower(), three.GetLower());
                    s1_up = AdvSimd.MultiplyWideningUpperAndAdd(s1_up, b1, three);

                    // Right shift by 2 (lower and upper).
                    b0 = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                            AdvSimd.ShiftRightLogicalRoundedNarrowingLower(s0_low, 2),
                            s0_up, 2);
                    b1 = AdvSimd.ShiftRightLogicalRoundedNarrowingUpper(
                            AdvSimd.ShiftRightLogicalRoundedNarrowingLower(s1_low, 2),
                            s1_up, 2);

                    // Store the 32 new elements to the output.
                    AdvSimd.Arm64.StoreVectorAndZip(output + i * 2, (b0, b1));
                }

                // Handle the remaining elements.
                for (; i < Size - 1; i++)
                {
                    ushort s1 = (ushort)input[i];
                    ushort s2 = (ushort)input[i + 1];
                    output[2 * i] = (byte)((3 * s1 + s2 + 2) >> 2);
                    output[2 * i + 1] = (byte)((3 * s2 + s1 + 2) >> 2);
                }
            }
        }

        [Benchmark]
        public unsafe void Sve2UpscaleFilter()
        {
            Vector<byte> pTrue = Sve.CreateTrueMaskByte();
            Vector<byte> three = new Vector<byte>(3);
            Vector<ushort> eight = new Vector<ushort>(8);

            fixed (byte* input = _input, output = _output)
            {
                int lmt = Size - 1;
                int i = 0;
                Vector<byte> pLoop = Sve.CreateWhileLessThanMask8Bit(0, lmt);
                while (Sve.TestAnyTrue(pTrue, pLoop))
                {
                    // Load two consecutive samples.
                    Vector<byte> b0 = Sve.LoadVector(pLoop, input + i);
                    Vector<byte> b1 = Sve.LoadVector(pLoop, input + i + 1);

                    // Widen 8-bit vectors into 16-bit vectors with extend and right-shift.
                    Vector<ushort> s0_low = Sve.ZeroExtend8((Vector<ushort>)(b1));
                    Vector<ushort> s0_up = Sve.ShiftRightLogical((Vector<ushort>)(b1), eight);
                    Vector<ushort> s1_low = Sve.ZeroExtend8((Vector<ushort>)(b0));
                    Vector<ushort> s1_up = Sve.ShiftRightLogical((Vector<ushort>)(b0), eight);

                    // Widened multiply by three and add to result (lower and upper).
                    s0_low = Sve2.MultiplyWideningEvenAndAdd(s0_low, b0, three);
                    s0_up = Sve2.MultiplyWideningOddAndAdd(s0_up, b0, three);
                    s1_low = Sve2.MultiplyWideningEvenAndAdd(s1_low, b1, three);
                    s1_up = Sve2.MultiplyWideningOddAndAdd(s1_up, b1, three);

                    // Right shift by 2 (lower and upper).
                    b0 = Sve2.ShiftRightLogicalRoundedNarrowingOdd(
                            Sve2.ShiftRightLogicalRoundedNarrowingEven(s0_low, 2),
                            s0_up, 2);
                    b1 = Sve2.ShiftRightLogicalRoundedNarrowingOdd(
                            Sve2.ShiftRightLogicalRoundedNarrowingEven(s1_low, 2),
                            s1_up, 2);

                    // Store the new elements to the output.
                    Sve.StoreAndZip(pLoop, output + i * 2, (b0, b1));

                    i += (int)Sve.Count8BitElements();
                    pLoop = Sve.CreateWhileLessThanMask8Bit(i, lmt);
                }
            }
        }
    }
}
