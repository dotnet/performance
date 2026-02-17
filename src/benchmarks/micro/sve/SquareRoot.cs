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
    [BenchmarkCategory(Categories.Runtime, Categories.Sve)]
    [OperatingSystemsArchitectureFilter(allowed: true, System.Runtime.InteropServices.Architecture.Arm64)]
    [Config(typeof(Config))]
    public class SquareRoot
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
        private float[] _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input = ValuesGenerator.Array<float>(Size);
            _output = new float[Size];
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            float[] current = (float[])_output.Clone();
            Setup();
            Scalar();
            float[] scalar = (float[])_output.Clone();
            // Check that the result is the same as scalar.
            for (int i = 0; i < Size; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (float* input = _input, output = _output)
            {
                for (int i = 0; i < Size; i++)
                {
                    output[i] = (float)Math.Sqrt(input[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void Vector128SquareRoot()
        {
            fixed (float* input = _input, output = _output)
            {
                int i = 0;
                for (; i <= Size - 4; i += 4)
                {
                    Vector128<float> inVec = AdvSimd.LoadVector128(input + i);
                    Vector128<float> outVec = AdvSimd.Arm64.Sqrt(inVec);
                    AdvSimd.Store(output + i, outVec);
                }
                // Handle tail.
                for (; i < Size; i++)
                {
                    output[i] = (float)Math.Sqrt(input[i]);
                }
            }
        }

        [Benchmark]
        public unsafe void SveSquareRoot()
        {
            fixed (float* input = _input, output = _output)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                // We use Vector<uint> for predicates since there are no Vector<float>
                // overloads for TestFirstTrue and CreateWhileLessThanMask etc.
                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();
                Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(0, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    // Since pLoop is a Vector<uint> predicate, we load the input as uint array,
                    // then cast it back to Vector<float>.
                    // This is preferable to casting pLoop to Vector<float>, which would cause
                    // an unnecessary conversion from predicate to vector in the codegen.
                    Vector<float> inVec = (Vector<float>)Sve.LoadVector(pLoop, (uint*)(input + i));
                    Vector<float> outVec = Sve.Sqrt(inVec);
                    Sve.StoreAndZip(pLoop, (uint*)output + i, (Vector<uint>)outVec);

                    // Handle loop.
                    i += cntw;
                    pLoop = Sve.CreateWhileLessThanMask32Bit(i, Size);
                }
            }
        }

        [Benchmark]
        public unsafe void SveTail()
        {
            fixed (float* input = _input, output = _output)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                Vector<float> pTrue = Sve.CreateTrueMaskSingle();
                for (; i <= Size - cntw; i += cntw)
                {
                    Vector<float> inVec = Sve.LoadVector(pTrue, input + i);
                    Vector<float> outVec = Sve.Sqrt(inVec);
                    Sve.StoreAndZip(pTrue, output + i, outVec);
                }
                // Handle tail.
                for (; i < Size; i++)
                {
                    output[i] = (float)Math.Sqrt(input[i]);
                }
            }
        }

    }
}
