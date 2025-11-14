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
    public class Clamp
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

        private int _output;

        [GlobalCleanup]
        public virtual void Verify()
        {
            int current = _output;
            Scalar();
            int scalar = _output;
            // Check that the result is the same as the scalar result.
            Debug.Assert(current == scalar);
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_040.c

        [Benchmark]
        public unsafe void Scalar()
        {
            int res = 0;
            int val = Size / 2;
            for (int i = 0; i < Size; i++)
            {
                res += Math.Min(Math.Max(val, i), 2 * i);
            }
            _output = res;
        }

        [Benchmark]
        public unsafe void Vector128Clamp()
        {
            int i = 0;
            int lmt = Size - Size % 4;
            int val = Size / 2;

            Vector128<int> resVec = Vector128<int>.Zero;
            Vector128<int> valVec = Vector128.Create(val);
            Vector128<int> minVec = Vector128.Create(0, 1, 2, 3);
            for (; i < lmt; i += 4)
            {
                Vector128<int> maxVec = AdvSimd.ShiftLeftLogical(minVec.AsUInt32(), 1).AsInt32();
                Vector128<int> tmpVec = AdvSimd.Min(AdvSimd.Max(valVec, minVec), maxVec);
                resVec = AdvSimd.Add(resVec, tmpVec);
                minVec = AdvSimd.Add(minVec, Vector128.Create(4));
            }
            int res = (int)AdvSimd.Arm64.AddAcross(resVec).ToScalar();
            for (; i < Size; i++)
            {
                res += Math.Min(Math.Max(val, i), 2 * i);
            }
            _output = res;
        }

        [Benchmark]
        public unsafe void SveClamp()
        {
            int i = 0;
            int length = Size;
            int cntw = (int)Sve.Count32BitElements();

            Vector<int> resVec = Vector<int>.Zero;
            Vector<int> valVec = new Vector<int>(Size / 2);
            Vector<int> minVec = Vector<int>.Indices;
            Vector<int> pTrue = Sve.CreateTrueMaskInt32();
            Vector<int> pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, length);
            while (Sve.TestFirstTrue(pTrue, pLoop))
            {
                Vector<int> maxVec = Sve.ShiftLeftLogical(minVec, Vector<uint>.One);
                Vector<int> tmpVec = Sve.Min(Sve.Max(valVec, minVec), maxVec);
                resVec = Sve.ConditionalSelect(pLoop, Sve.Add(resVec, tmpVec), resVec);
                minVec = Sve.Add(minVec, new Vector<int>(cntw));

                i += cntw;
                pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, length);
            }
            _output = (int)Sve.AddAcross(resVec).ToScalar();
        }

        [Benchmark]
        public unsafe void SveTail()
        {
            int i = 0;
            int length = Size;
            int cntw = (int)Sve.Count32BitElements();

            Vector<int> resVec = Vector<int>.Zero;
            Vector<int> valVec = new Vector<int>(Size / 2);
            Vector<int> minVec = Vector<int>.Indices;
            for (; i < length; i += cntw)
            {
                Vector<int> pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, length);
                Vector<int> maxVec = Sve.ShiftLeftLogical(minVec, Vector<uint>.One);
                Vector<int> tmpVec = Sve.Min(Sve.Max(valVec, minVec), maxVec);
                resVec = Sve.ConditionalSelect(pLoop, Sve.Add(resVec, tmpVec), resVec);
                minVec = Sve.Add(minVec, new Vector<int>(cntw));
            }
            _output = (int)Sve.AddAcross(resVec).ToScalar();
        }
    }
}
