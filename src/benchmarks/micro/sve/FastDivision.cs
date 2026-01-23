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
    public class FastDivision
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

        private double[] _input1;
        private double[] _input2;
        private double[] _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input1 = new double[Size];
            _input2 = new double[Size];

            double[] vals = ValuesGenerator.Array<double>(Size * 2);
            for (int i = 0; i < Size; i++)
            {
                _input1[i] = vals[i];
                _input2[i] = vals[Size + i];
            }
            _output = new double[Size];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            double[] current = (double[])_output.Clone();
            Setup();
            Scalar();
            double[] scalar = (double[])_output.Clone();

            // Check that the result is the same as scalar (within 3ULP).
            for (int i = 0; i < Size; i++)
            {
                int e = (int)(BitConverter.DoubleToUInt64Bits(scalar[i]) >> 52 & 0x7ff);
                if (e == 0) e++;
                double ulpScale = Math.ScaleB(1.0f, e - 1023 - 52);
                double ulpError = Math.Abs(current[i] - scalar[i]) / ulpScale;
                Debug.Assert(ulpError <= 3);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_028.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (double* input1 = _input1, input2 = _input2, output = _output)
            {
                for (int i = 0; i < Size; i++)
                {
                    output[i] = input1[i] / input2[i];
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128FastDivision()
        {
            fixed (double* input1 = _input1, input2 = _input2, output = _output)
            {
                int i = 0;
                for (; i <= Size - 2; i += 2)
                {
                    Vector128<double> input1Vec = AdvSimd.LoadVector128(input1 + i);
                    Vector128<double> input2Vec = AdvSimd.LoadVector128(input2 + i);

                    // Estimate the reciprocal of 1/input2Vec.
                    Vector128<double> input2VecInv = AdvSimd.Arm64.ReciprocalEstimate(input2Vec);

                    // Iteratively refine the estimation by multiplying the reicrocal step.
                    Vector128<double> stp2;
                    for (int j = 0; j < 3; j++)
                    {
                        stp2 = AdvSimd.Arm64.ReciprocalStep(input2Vec, input2VecInv);
                        input2VecInv = AdvSimd.Arm64.Multiply(input2VecInv, stp2);
                    }

                    // Get the result of input1Vec * (1/input2Vec)
                    Vector128<double> outVec = AdvSimd.Arm64.Multiply(input2VecInv, input1Vec);
                    AdvSimd.Store(output + i, outVec);
                }
                // Handle tail.
                for (; i < Size; i++)
                {
                    output[i] = input1[i] / input2[i];
                }
            }
        }

        [Benchmark]
        public unsafe void SveFastDivision()
        {
            fixed (double* input1 = _input1, input2 = _input2, output = _output)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();

                Vector<ulong> pTrue = Sve.CreateTrueMaskUInt64();
                Vector<ulong> pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<double> input1Vec = (Vector<double>)Sve.LoadVector(pLoop, (ulong*)input1 + i);
                    Vector<double> input2Vec = (Vector<double>)Sve.LoadVector(pLoop, (ulong*)input2 + i);

                    // Estimate the reciprocal of 1/input2Vec.
                    Vector<double> input2VecInv = Sve.ReciprocalEstimate(input2Vec);

                    // Iteratively refine the estimation by multiplying the reicrocal step.
                    Vector<double> stp2;
                    for (int j = 0; j < 3; j++)
                    {
                        stp2 = Sve.ReciprocalStep(input2Vec, input2VecInv);
                        input2VecInv = Sve.Multiply(input2VecInv, stp2);
                    }

                    // Get the result of input1Vec * (1/input2Vec)
                    Vector<double> outVec = Sve.Multiply(input2VecInv, input1Vec);
                    Sve.StoreAndZip(pLoop, (ulong*)output + i, (Vector<ulong>)outVec);

                    i += cntd;
                    pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                }
            }
        }

    }
}
