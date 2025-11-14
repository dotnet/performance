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
    public class FP64Overflow
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

        private double[] _input1;
        private double[] _input2;
        private double[] _output;
        private long[] _exponent;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input1 = new double[Size];
            _input2 = new double[Size];
            double[] vals = ValuesGenerator.Array<double>(Size * 2);
            for (int i = 0; i < Size; i++)
            {
                _input1[i] = vals[i] + Double.MinValue;
                _input2[i] = vals[Size + i];
            }
            _output = new double[Size];
            _exponent = new long[Size];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            double[] current = (double[])_output.Clone();
            long[] current_exp = (long[])_exponent.Clone();
            Setup();
            Scalar();
            double[] scalar = (double[])_output.Clone();
            long[] scalar_exp = (long[])_exponent.Clone();

            // Check that the result is the same as scalar.
            for (int i = 0; i < Size; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
                Debug.Assert(current_exp[i] == scalar_exp[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_111.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (double* input1 = _input1, input2 = _input2, output = _output)
            fixed (long* exponent = _exponent)
            {
                long mask = 1023;

                for (int i = 0; i < Size; i++)
                {
                    // Convert element from double to ulong.
                    ulong in1Bits = *(ulong*)&input1[i];
                    // Extract the exponent bits by shifting left by 1 then right by 53.
                    long exp = (long)((in1Bits << 1) >> 53);
                    long scale = mask - exp;
                    output[i] = Math.ScaleB(input1[i], (int)scale);
                    output[i] *= input2[i];
                    // Calculate exponent offset.
                    exponent[i] = exp - mask;
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128FP64Overflow()
        {
            fixed (double* input1 = _input1, input2 = _input2, output = _output)
            fixed (long* exponent = _exponent)
            {
                int i = 0;
                long mask = 1023;
                Vector128<long> maskVec = Vector128.Create(mask);
                Vector128<long> scaleMask = Vector128.Create(~(2047L << 52));

                for (; i <= Size - 2; i += 2)
                {
                    Vector128<double> in1Vec = AdvSimd.LoadVector128(input1 + i);
                    Vector128<double> in2Vec = AdvSimd.LoadVector128(input2 + i);
                    Vector128<long> in1Bits = in1Vec.AsInt64();

                    // Extract the exponent bits by shifting left by 1 then right by 53.
                    Vector128<long> exp = AdvSimd.ShiftRightLogical(AdvSimd.ShiftLeftLogical(in1Vec.AsUInt64(), 1), 53).AsInt64();
                    Vector128<long> scale = AdvSimd.Subtract(maskVec, exp);

                    // Calculate Scale(in1Vec, scale).
                    scale = AdvSimd.ShiftLeftLogical(scale, 52);
                    Vector128<long> outBits = AdvSimd.Add(in1Bits, scale);
                    in1Bits = AdvSimd.And(in1Bits, scaleMask);
                    outBits = AdvSimd.Or(in1Bits, outBits);
                    Vector128<double> outVec = outBits.AsDouble();
                    outVec = AdvSimd.Arm64.Multiply(outVec, in2Vec);

                    // Store result to output array.
                    AdvSimd.Store(output + i, outVec);

                    // Calculate exponent offset.
                    exp = AdvSimd.Subtract(exp, maskVec);
                    // Store result to exponent array.
                    AdvSimd.Store(exponent + i, exp);
                }
                // Handle tail.
                for (; i < Size; i++)
                {
                    ulong in1Bits = *(ulong*)&input1[i];
                    long exp = (long)((in1Bits << 1) >> 53);
                    long scale = mask - exp;
                    output[i] = Math.ScaleB(input1[i], (int)scale);
                    output[i] *= input2[i];
                    exponent[i] = exp - mask;
                }
            }
        }

        [Benchmark]
        public unsafe void SveFP64Overflow()
        {
            fixed (double* input1 = _input1, input2 = _input2, output = _output)
            fixed (long* exponent = _exponent)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();

                Vector<long> maskVec = new Vector<long>(1023);

                Vector<ulong> pTrue = Sve.CreateTrueMaskUInt64();
                Vector<ulong> pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Load Vector<input1> as ulong then convert to Vector<double>.
                    Vector<ulong> in1Bits = Sve.LoadVector(pLoop, (ulong*)input1 + i);
                    Vector<double> in1Vec = (Vector<double>)in1Bits;
                    Vector<double> in2Vec = (Vector<double>)Sve.LoadVector(pLoop, (ulong*)input2 + i);

                    // Extract the exponent bits by shifting left by 1 then right by 53.
                    Vector<long> exp = (Vector<long>)Sve.ShiftRightLogical(Sve.ShiftLeftLogical(in1Bits, new Vector<ulong>(1)), new Vector<ulong>(53));

                    // Compute the output.
                    Vector<long> scale = Sve.Subtract(maskVec, exp);
                    Vector<double> outVec = Sve.Scale(in1Vec, scale);
                    outVec = Sve.Multiply(outVec, in2Vec);
                    // Store result to output array.
                    Sve.StoreAndZip(pLoop, (ulong*)output + i, (Vector<ulong>)outVec);

                    // Calculate exponent offset.
                    exp = Sve.Subtract(exp, maskVec);
                    // Store result to exponent array.
                    Sve.StoreAndZip(pLoop, (ulong*)exponent + i, (Vector<ulong>)exp);

                    // Handle loop.
                    i += cntd;
                    pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                }
            }
        }

        [Benchmark]
        public unsafe void Sve2FP64Overflow()
        {
            fixed (double* input1 = _input1, input2 = _input2, output = _output)
            fixed (long* exponent = _exponent)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();

                Vector<ulong> pTrue = Sve.CreateTrueMaskUInt64();
                Vector<ulong> pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Load input vectors.
                    Vector<double> in1Vec = (Vector<double>)Sve.LoadVector(pLoop, (ulong*)input1 + i);
                    Vector<double> in2Vec = (Vector<double>)Sve.LoadVector(pLoop, (ulong*)input2 + i);

                    // Get the exponent by taking log.
                    Vector<long> exp = Sve2.Log2(in1Vec);

                    // Compute the output.
                    Vector<long> scale = Sve.Negate(exp);
                    Vector<double> outVec = Sve.Scale(in1Vec, scale);
                    outVec = Sve.Multiply(outVec, in2Vec);
                    // Store result to output array.
                    Sve.StoreAndZip(pLoop, (ulong*)output + i, (Vector<ulong>)outVec);
                    // Store result to exponent array.
                    Sve.StoreAndZip(pLoop, (ulong*)exponent + i, (Vector<ulong>)exp);

                    // Handle loop.
                    i += cntd;
                    pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                }
            }
        }

    }
}
