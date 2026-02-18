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
    public class MultiplyPow2
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

        private double[] _input;
        private long[] _scale;
        private double[] _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input = ValuesGenerator.Array<double>(Size);
            _scale = ValuesGenerator.Array<long>(Size);

            for (int i = 0; i < Size; i++)
            {
                // Set the scale to within the range of [-128, 128).
                _scale[i] = _scale[i] % 256 - 128;
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

            // Check that the result is the same as scalar.
            for (int i = 0; i < Size; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_029.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (double* input = _input, output = _output)
            fixed (long* scale = _scale)
            {
                for (int i = 0; i < Size; i++)
                {
                    output[i] = Math.ScaleB(input[i], (int)scale[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128MultiplyPow2()
        {
            fixed (double* input = _input, output = _output)
            fixed (long* scale = _scale)
            {
                Vector128<long> mask = Vector128.Create(~(2047L << 52));

                int i = 0;
                for (; i <= Size - 2; i += 2)
                {
                    Vector128<long> inVec = AdvSimd.LoadVector128((long*)input + i);
                    Vector128<long> scaleVec = AdvSimd.LoadVector128(scale + i);

                    scaleVec = AdvSimd.ShiftLeftLogical(scaleVec, 52);

                    Vector128<long> outVec = AdvSimd.Add(inVec, scaleVec);
                    inVec = AdvSimd.And(inVec, mask);
                    outVec = AdvSimd.Or(inVec, outVec);

                    AdvSimd.Store((long*)output + i, outVec);
                }
                // Handle tail.
                for (; i < Size; i++)
                {
                    output[i] = Math.ScaleB(input[i], (int)scale[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void SveMultiplyPow2()
        {
            fixed (double* input = _input, output = _output)
            fixed (long* scale = _scale)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();

                Vector<ulong> pTrue = Sve.CreateTrueMaskUInt64();
                Vector<ulong> pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Cast the array pointers to ulong so the predicate can be shared.
                    // Avoid casting predicate vectors.
                    Vector<double> inVec = (Vector<double>)Sve.LoadVector(pLoop, (ulong*)input + i);
                    Vector<long> scaleVec = (Vector<long>)Sve.LoadVector(pLoop, (ulong*)scale + i);

                    Vector<double> outVec = Sve.Scale(inVec, scaleVec);
                    Sve.StoreAndZip(pLoop, (ulong*)output + i, (Vector<ulong>)outVec);

                    // Handle loop.
                    i += cntd;
                    pLoop = Sve.CreateWhileLessThanMask64Bit(i, Size);
                }
            }
        }

        [Benchmark]
        public unsafe void SveTail()
        {
            fixed (double* input = _input, output = _output)
            fixed (long* scale = _scale)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();

                Vector<long> pTrue = Sve.CreateTrueMaskInt64();
                Vector<double> pTrueD = Sve.CreateTrueMaskDouble();
                for (; i <= Size - cntd; i += cntd)
                {
                    Vector<double> inVec = Sve.LoadVector(pTrueD, input + i);
                    Vector<long> scaleVec = Sve.LoadVector(pTrue, scale + i);

                    Vector<double> outVec = Sve.Scale(inVec, scaleVec);
                    Sve.StoreAndZip(pTrueD, output + i, outVec);
                }
                // Handle tail.
                for (; i < Size; i++)
                {
                    output[i] = Math.ScaleB(input[i], (int)scale[i]);
                }
            }
        }

    }
}
