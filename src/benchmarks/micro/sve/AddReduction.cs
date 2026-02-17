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
    public class AddReduction
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

        private double[] _source;
        private double _result;

        [GlobalSetup]
        public virtual void Setup()
        {
            _source = ValuesGenerator.Array<double>(Size);
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            double current = _result;
            Setup();
            Scalar();
            double scalar = _result;
            // Check that the result is the same as scalar (within 10ULP).
            // Error is due to rounding floating-point additions in different
            // orderings (for Vector128AddReduction and SveAddReduction).
            // SveAddSequential has the same ordering as Scalar.
            int e = (int)(BitConverter.DoubleToUInt64Bits(scalar) >> 52 & 0x7ff);
            if (e == 0) e++;
            double ulpScale = Math.ScaleB(1.0f, e - 1023 - 52);
            double ulpError = Math.Abs(current - scalar) / ulpScale;
            Debug.Assert(ulpError <= 10);
        }

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (double* a = _source)
            {
                double res = 0.0;
                for (int i = 0; i < Size; i++)
                {
                    res += a[i];
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void Vector128AddReduction()
        {
            fixed (double* a = _source)
            {
                int i = 0;
                double res = 0.0;
                for (; i <= Size - 2; i += 2)
                {
                    Vector128<double> data = AdvSimd.LoadVector128(a + i);
                    // Sum up all lanes and reduce to scalar.
                    res += AdvSimd.Arm64.AddPairwiseScalar(data).ToScalar();
                }
                // Handle Tail.
                for (; i < Size; i++)
                {
                    res += a[i];
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void SveAddSequential()
        {
            fixed (double* a = _source)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();

                Vector<double> resVec = Vector<double>.Zero;
                Vector<double> pTrue = Sve.CreateTrueMaskDouble();
                for (; i <= Size - cntd; i += cntd)
                {
                    Vector<double> data = Sve.LoadVector(pTrue, a + i);
                    // Sum up all lanes sequentially and add to scalar.
                    resVec = Sve.AddSequentialAcross(resVec, data);
                }
                // Get the scalar result from the first lane.
                double res = resVec.ToScalar();
                // Handle Tail.
                for (; i < Size; i++)
                {
                    res += a[i];
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void SveAddReduction()
        {
            fixed (double* a = _source)
            {
                int i = 0;
                int cntd = (int)Sve.Count64BitElements();
                double res = 0.0;

                Vector<double> pTrue = Sve.CreateTrueMaskDouble();
                for (; i <= Size - cntd; i += cntd)
                {
                    Vector<double> data = Sve.LoadVector(pTrue, a + i);
                    // Sum up all lanes and reduce to scalar.
                    res += Sve.AddAcross(data).ToScalar();
                }
                // Handle Tail.
                for (; i < Size; i++)
                {
                    res += a[i];
                }
                _result = res;
            }
        }
    }
}
