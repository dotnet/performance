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
    public class VectorMax
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddFilter(new SimpleFilter(_ => Sve.IsSupported));
            }
        }

        [Params(15, 127, 527, 10015)]
        public short Size;

        private short[] _input;
        private uint _output;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input = ValuesGenerator.Array<short>(Size);
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            uint current = _output;
            Setup();
            Scalar();
            uint scalar = _output;
            // Check that the result is the same as the scalar result.
            Debug.Assert(current == scalar);
        }

        // The following algorithms are adapted from Arm "SVE Programming Examples":
        // https://developer.arm.com/documentation/dai0548/latest/ (example B1)

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (short* input = _input)
            {
                short maxVal = input[0];
                short maxIdx = 0;
                for (short i = 0; i < Size; i++)
                {
                    if (input[i] > maxVal)
                    {
                        maxVal = input[i];
                        maxIdx = i;
                    }
                }
                // Combine max value and index into a 32-bit integer.
                _output = (uint)maxVal << 16 ^ (uint)maxIdx;
            }
        }

        [Benchmark]
        public unsafe void Vector128VectorMax()
        {
            fixed (short* input = _input)
            {
                short i = 0;
                short lmt = (short)(Size - Size % 8);

                Vector128<short> idxVec = Vector128.Create(0, 1, 2, 3, 4, 5, 6, 7);

                // Initialize the first vector worth of values.
                Vector128<short> maxVec = Vector128.Load(input);
                Vector128<short> maxIdxVec = idxVec;

                i += 8;
                for (; i < lmt; i += 8)
                {
                    Vector128<short> val = Vector128.Load(input + i);
                    idxVec = AdvSimd.Add(idxVec, Vector128.Create((short)8));
                    // Find indices of the new maximum values.
                    Vector128<short> cmp = AdvSimd.CompareGreaterThan(val, maxVec);
                    // Update maximum values.
                    maxVec = AdvSimd.Max(maxVec, val);
                    // Update the indices with the maximum values.
                    maxIdxVec = AdvSimd.BitwiseSelect(cmp, idxVec, maxIdxVec);
                }

                // Get the maximum element across the max vector.
                short maxVal = AdvSimd.Arm64.MaxAcross(maxVec).ToScalar();

                // Find the first occurence (min index) of the max value.
                Vector128<short> cmpIndex = AdvSimd.CompareEqual(maxVec, Vector128.Create(maxVal));
                maxIdxVec = AdvSimd.BitwiseSelect(cmpIndex, maxIdxVec, Vector128.Create((short)-1));
                short maxIdx = (short)AdvSimd.Arm64.MinAcross(maxIdxVec.AsUInt16()).ToScalar();

                // Search in remaining elements.
                for (; i < Size; i++)
                {
                    if (input[i] > maxVal)
                    {
                        maxVal = input[i];
                        maxIdx = i;
                    }
                }

                // Combine max value and index into a 32-bit integer.
                _output = (uint)maxVal << 16 ^ (uint)maxIdx;
            }
        }

        [Benchmark]
        public unsafe void SveVectorMax()
        {
            fixed (short* input = _input)
            {
                short i = 0;
                short cnth = (short)Sve.Count16BitElements();

                Vector<short> pTrue = Sve.CreateTrueMaskInt16();
                Vector<short> pLoop = (Vector<short>)Sve.CreateWhileLessThanMask16Bit(0, Size);
                Vector<short> idxVec = Vector<short>.Indices;

                // Initialize the first vector worth of values.
                Vector<short> maxVec = Sve.LoadVector(pLoop, input);
                Vector<short> maxIdxVec = idxVec;

                i += cnth;
                pLoop = (Vector<short>)Sve.CreateWhileLessThanMask16Bit(i, Size);
                while (Sve.TestFirstTrue(pTrue, pLoop))
                {
                    Vector<short> val = Sve.LoadVector(pLoop, input + i);
                    // Increment indices counter.
                    idxVec = Sve.Add(idxVec, new Vector<short>(cnth));
                    // Find indices of the new maximum values.
                    Vector<short> cmp = Sve.CompareGreaterThan(val, maxVec);
                    // Update maximum values.
                    maxVec = Sve.Max(maxVec, val);
                    // Update the indices with the maximum values.
                    maxIdxVec = Sve.ConditionalSelect(cmp, idxVec, maxIdxVec);

                    // Handle loop.
                    i += cnth;
                    pLoop = (Vector<short>)Sve.CreateWhileLessThanMask16Bit(i, Size);
                }

                // Get the maximum element across the max vector.
                short maxVal = Sve.MaxAcross(maxVec).ToScalar();

                // Find the first occurence (min index) of the max value.
                Vector<short> pIndex = Sve.CompareEqual(maxVec, new Vector<short>(maxVal));
                maxIdxVec = Sve.ConditionalSelect(pIndex, maxIdxVec, new Vector<short>(-1));
                short maxIdx = (short)Sve.MinAcross((Vector<ushort>)maxIdxVec).ToScalar();

                // Combine max value and index into a 32-bit integer.
                _output = (uint)maxVal << 16 ^ (uint)maxIdx;
            }
        }

    }
}
