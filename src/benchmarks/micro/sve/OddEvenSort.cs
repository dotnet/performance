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
    public class OddEvenSort
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

        private uint[] _source;

        [GlobalSetup]
        public virtual void Setup()
        {
            _source = ValuesGenerator.Array<uint>(Size);
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            uint[] current = (uint[])_source.Clone();
            Setup();
            Scalar();
            uint[] scalar = (uint[])_source.Clone();

            // Check that the result is the same as the scalar result.
            for (int i = 0; i < current.Length; i++)
            {
                Debug.Assert(current[i] == scalar[i]);
            }
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_122.c

        [Benchmark]
        public unsafe void Scalar()
        {
            fixed (uint* source = _source)
            {
                bool sorted;
                do
                {
                    sorted = true;
                    // Do even pass.
                    for (int i = 1; i < Size; i += 2)
                    {
                        if (source[i - 1] > source[i])
                        {
                            // Swap source[i - 1] and source[i].
                            uint tmp = source[i - 1];
                            source[i - 1] = source[i];
                            source[i] = tmp;
                            sorted = false;
                        }
                    }

                    // Do odd pass.
                    for (int i = 1; i < Size - 1; i += 2)
                    {
                        if (source[i] > source[i + 1])
                        {
                            // Swap source[i] and source[i + 1].
                            uint tmp = source[i];
                            source[i] = source[i + 1];
                            source[i + 1] = tmp;
                            sorted = false;
                        }
                    }
                } while (!sorted);
            }
        }

        [Benchmark]
        public unsafe void Vector128OddEvenSort()
        {
            fixed (uint* source = _source)
            {
                int n = Size;
                int oddeven = 0;
                int sorted = 0;

                do
                {
                    // Increment sort counter.
                    sorted++;
                    // Start from odd or even elements in turns.
                    int j = oddeven;
                    for (; j < n - 8; j += 8)
                    {
                        // Interleaved load elements.
                        (Vector128<uint> a0, Vector128<uint> a1) = AdvSimd.Arm64.Load2xVector128AndUnzip(source + j);

                        // Find elements that are not in order.
                        Vector128<uint> cmp = AdvSimd.CompareGreaterThanOrEqual(a0, a1);
                        // Swap those elements.
                        Vector128<uint> b0 = AdvSimd.BitwiseSelect(cmp, a1, a0);
                        Vector128<uint> b1 = AdvSimd.BitwiseSelect(cmp, a0, a1);

                        // Check if there are any swaps.
                        ulong cmpMax = AdvSimd.Arm64.MaxPairwise(cmp, cmp).AsUInt64().ToScalar();
                        if (cmpMax != 0)
                        {
                            // Reset sorted counter to 0.
                            sorted = 0;
                        }

                        // Interleaved store back into the array.
                        AdvSimd.Arm64.StoreVectorAndZip(source + j, (b0, b1));
                    }

                    // Handle remaining elemnts in scalar.
                    for (; j < n; j += 2)
                    {
                        if (source[j - 1] > source[j])
                        {
                            // Swap source[j - 1] and source[j].
                            uint tmp = source[j - 1];
                            source[j - 1] = source[j];
                            source[j] = tmp;
                            sorted = 0;
                        }
                    }
                    for (; j < n - 1; j += 2)
                    {
                        if (source[j] > source[j + 1])
                        {
                            // Swap source[j] and source[j + 1].
                            uint tmp = source[j - 1];
                            source[j - 1] = source[j];
                            source[j] = tmp;
                            sorted = 0;
                        }
                    }

                    // Flip the odd-even flag.
                    oddeven ^= 1;
                // Repeat until we see two consecutive (even and odd) iterations without swaps.
                } while (sorted < 2);
            }
        }

        [Benchmark]
        public unsafe void SveOddEvenSort()
        {
            fixed (uint* source = _source)
            {
                int n = Size;
                int cntw = (int)Sve.Count32BitElements();
                int sorted = 0;
                int oddeven = 0;

                do
                {
                    // Increment sort counter.
                    sorted++;
                    // Start from odd or even elements in turns.
                    int j = oddeven;
                    for (; j < n - 1; j += (cntw << 1))
                    {
                        // Get predicate for elements to load/store.
                        Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(0, (n - j) / 2);
                        // Interleaved load elements.
                        (Vector<uint> a0, Vector<uint> a1) = Sve.Load2xVectorAndUnzip(pLoop, source + j);

                        // Find elements that are not in order.
                        Vector<uint> pCmp = Sve.ConditionalSelect(pLoop, Sve.CompareGreaterThanOrEqual(a0, a1), Sve.CreateFalseMaskUInt32());
                        // Swap those elements.
                        Vector<uint> b0 = Sve.ConditionalSelect(pCmp, a1, a0);
                        Vector<uint> b1 = Sve.ConditionalSelect(pCmp, a0, a1);

                        // Check if there are any swaps.
                        if (Sve.TestAnyTrue(pLoop, pCmp))
                        {
                            // Reset sorted counter to 0.
                            sorted = 0;
                        }

                        // Interleaved store back into the array.
                        Sve.StoreAndZip(pLoop, source + j, (b0, b1));
                    }
                    // Flip the odd-even flag.
                    oddeven ^= 1;
                // Repeat until we see two consecutive (even and odd) iterations without swaps.
                } while (sorted < 2);
            }
        }

        [Benchmark]
        public unsafe void SveTail()
        {
            fixed (uint* source = _source)
            {
                int n = Size;
                int cntw = (int)Sve.Count32BitElements();
                int oddeven = 0;
                int sorted = 0;

                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();
                do
                {
                    // Increment sort counter.
                    sorted++;
                    // Start from odd or even elements in turns.
                    int j = oddeven;
                    for (; j < n - (cntw << 1); j += (cntw << 1))
                    {
                        // Interleaved load elements.
                        (Vector<uint> a0, Vector<uint> a1) = Sve.Load2xVectorAndUnzip(pTrue, source + j);

                        // Find elements that are not in order.
                        Vector<uint> pCmp = Sve.CompareGreaterThanOrEqual(a0, a1);
                        // Swap those elements.
                        Vector<uint> b0 = Sve.ConditionalSelect(pCmp, a1, a0);
                        Vector<uint> b1 = Sve.ConditionalSelect(pCmp, a0, a1);

                        // Check if there are any swaps.
                        if (Sve.TestAnyTrue(pTrue, pCmp))
                        {
                            // Reset sorted counter to 0.
                            sorted = 0;
                        }

                        // Interleaved store back into the array.
                        Sve.StoreAndZip(pTrue, source + j, (b0, b1));
                    }
                    // Handle tail using predicates.
                    for (; j < n - 1; j += (cntw << 1))
                    {
                        Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(0, (n - j) / 2);
                        (Vector<uint> a0, Vector<uint> a1) = Sve.Load2xVectorAndUnzip(pLoop, source + j);

                        Vector<uint> pCmp = Sve.ConditionalSelect(pLoop, Sve.CompareGreaterThanOrEqual(a0, a1), Sve.CreateFalseMaskUInt32());
                        Vector<uint> b0 = Sve.ConditionalSelect(pCmp, a1, a0);
                        Vector<uint> b1 = Sve.ConditionalSelect(pCmp, a0, a1);

                        if (Sve.TestAnyTrue(pLoop, pCmp))
                        {
                            sorted = 0;
                        }

                        Sve.StoreAndZip(pLoop, source + j, (b0, b1));
                    }
                    // Flip the odd-even flag.
                    oddeven ^= 1;
                // Repeat until we see two consecutive (even and odd) iterations without swaps.
                } while (sorted < 2);
            }
        }

    }
}
