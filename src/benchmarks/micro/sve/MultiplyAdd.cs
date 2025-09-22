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
    public class MultiplyAdd
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

        private int[] _source1;
        private int[] _source2;
        private int _result;

        [GlobalSetup]
        public virtual void Setup()
        {
            _source1 = ValuesGenerator.Array<int>(Size);
            _source2 = ValuesGenerator.Array<int>(Size);
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            int current = _result;
            Setup();
            Scalar();
            int scalar = _result;
            // Check that the result is the same as the scalar result.
            Debug.Assert(current == scalar);
        }

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (int* a = _source1, b = _source2)
            {
                int res = 0;
                for (int i = 0; i < Size; i++)
                {
                    res += (int)(a[i] * b[i]);
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void Vector128MultiplyAdd()
        {
            fixed (int* a = _source1, b = _source2)
            {
                int incr = sizeof(Vector128<int>) / sizeof(int);
                int i = 0;

                // The length of the tail is Size modulo 4 * element count.
                int lmt = Size - (Size % (incr << 2));

                Vector128<int> res1 = Vector128<int>.Zero;
                Vector128<int> res2 = Vector128<int>.Zero;
                Vector128<int> res3 = Vector128<int>.Zero;
                Vector128<int> res4 = Vector128<int>.Zero;

                for (; i < lmt; i += incr << 2)
                {
                    // Unroll loop by 4.
                    Vector128<int> aVec1 = AdvSimd.LoadVector128(a + i);
                    Vector128<int> bVec1 = AdvSimd.LoadVector128(b + i);
                    Vector128<int> aVec2 = AdvSimd.LoadVector128(a + i + incr);
                    Vector128<int> bVec2 = AdvSimd.LoadVector128(b + i + incr);
                    Vector128<int> aVec3 = AdvSimd.LoadVector128(a + i + incr * 2);
                    Vector128<int> bVec3 = AdvSimd.LoadVector128(b + i + incr * 2);
                    Vector128<int> aVec4 = AdvSimd.LoadVector128(a + i + incr * 3);
                    Vector128<int> bVec4 = AdvSimd.LoadVector128(b + i + incr * 3);

                    // Calculate 4 vectors at a time.
                    res1 = AdvSimd.MultiplyAdd(res1, aVec1, bVec1);
                    res2 = AdvSimd.MultiplyAdd(res2, aVec2, bVec2);
                    res3 = AdvSimd.MultiplyAdd(res3, aVec3, bVec3);
                    res4 = AdvSimd.MultiplyAdd(res4, aVec4, bVec4);
                }

                // Sum all the results between the 4 vectors.
                res1 = Vector128.Add(res1, res2);
                res3 = Vector128.Add(res3, res4);
                res1 = Vector128.Add(res1, res3);
                int res = AdvSimd.Arm64.AddAcross(res1).ToScalar();

                // Process any remaining elements.
                for (; i < Size; i++)
                {
                    res += (int)(a[i] * b[i]);
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void SveMultiplyAdd()
        {
            fixed (int* a = _source1, b = _source2)
            {
                int i = 0;
                int cntw = (int)Sve.Count32BitElements();

                // The length of the tail is Size modulo 4 * element count.
                int lmt = (int)Size - (Size % (cntw << 2));

                Vector<int> res1 = Vector<int>.Zero;
                Vector<int> res2 = Vector<int>.Zero;
                Vector<int> res3 = Vector<int>.Zero;
                Vector<int> res4 = Vector<int>.Zero;
                Vector<int> pTrue = Sve.CreateTrueMaskInt32();

                while (i < lmt)
                {
                    // Unroll loop by 4.
                    Vector<int> aVec1 = Sve.LoadVector(pTrue, a + i);
                    Vector<int> bVec1 = Sve.LoadVector(pTrue, b + i);
                    Vector<int> aVec2 = Sve.LoadVector(pTrue, a + i + cntw);
                    Vector<int> bVec2 = Sve.LoadVector(pTrue, b + i + cntw);
                    Vector<int> aVec3 = Sve.LoadVector(pTrue, a + i + cntw * 2);
                    Vector<int> bVec3 = Sve.LoadVector(pTrue, b + i + cntw * 2);
                    Vector<int> aVec4 = Sve.LoadVector(pTrue, a + i + cntw * 3);
                    Vector<int> bVec4 = Sve.LoadVector(pTrue, b + i + cntw * 3);

                    // Calculate 4 vectors at a time.
                    res1 = Sve.MultiplyAdd(res1, aVec1, bVec1);
                    res2 = Sve.MultiplyAdd(res2, aVec2, bVec2);
                    res3 = Sve.MultiplyAdd(res3, aVec3, bVec3);
                    res4 = Sve.MultiplyAdd(res4, aVec4, bVec4);

                    // Increment counter by 4 times the element count.
                    i = Sve.SaturatingIncrementBy32BitElementCount(i, 4);
                }

                // Handle remaining elements using predicates.
                lmt = Size;
                Vector<int> pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, lmt);
                while (Sve.TestAnyTrue(pTrue, pLoop))
                {
                    Vector<int> aVec = Sve.LoadVector(pLoop, a + i);
                    Vector<int> bVec = Sve.LoadVector(pLoop, b + i);

                    // Apply pLoop mask on the result.
                    res1 = Sve.ConditionalSelect(pLoop, Sve.MultiplyAdd(res1, aVec, bVec), res1);

                    // Increment by a vector length.
                    i += cntw;
                    pLoop = (Vector<int>)Sve.CreateWhileLessThanMask32Bit(i, lmt);
                }

                // Sum up all elements in the 4 result vectors.
                res1 = Sve.Add(res1, res2);
                res3 = Sve.Add(res3, res4);
                _result = (int)Sve.AddAcross(Sve.Add(res1, res3)).ToScalar();
            }
        }

    }
}
