using System;
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
    public class Partition
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

        private uint[] _input;
        private uint[] _left;
        private uint[] _right;

        [GlobalSetup]
        public virtual void Setup()
        {
            _input = ValuesGenerator.Array<uint>(Size);
            _left  = new uint[Size];
            _right = new uint[Size];
        }

        [Benchmark]
        public unsafe ulong Scalar()
        {
            long i = 0;

            // Position within the output arrays.
            ulong indexLeft = 0;
            ulong indexRight = 0;

            fixed (uint* input = _input, left = _left, right = _right)
            {
                uint first = input[0];

                for (i = 0; i < Size; i++)
                {
                    if (input[i] < first)
                    {
                        left[indexLeft] = input[i];
                        indexLeft++;
                    }
                    else
                    {
                        right[indexRight] = input[i];
                        indexRight++;
                    }
                }

                return indexRight;
            }
        }

        [Benchmark]
        public unsafe ulong SvePartition()
        {
            fixed (uint* input = _input, left = _left, right = _right)
            {
                long i = 0;

                ulong indexLeft = 0;
                ulong indexRight = 0;

                Vector<uint> ones = Vector<uint>.One;

                Vector<uint> firstElemVec = Sve.DuplicateSelectedScalarToVector(
                    Sve.LoadVector(Sve.CreateTrueMaskUInt32(), input), 0
                );

                // Create a predicate for the loop.
                Vector<uint> pLoop = Sve.CreateWhileLessThanMask32Bit(i, Size);

                while (Sve.TestAnyTrue(Sve.CreateTrueMaskUInt32(), pLoop))
                {
                    // Load from the input array based on the loop predicate.
                    Vector<uint> data = Sve.LoadVector(pLoop, input + i);

                    // Predicate for elements in input array less than the first element.
                    Vector<uint> pCompare = Sve.CompareLessThan(data, firstElemVec);

                    // Apply the pLoop mask.
                    Vector<uint> pInner = Sve.ConditionalSelect(pLoop, pCompare, Vector<uint>.Zero);

                    // Squash all found elements to the lower lanes of the vector.
                    Vector<uint> compacted = Sve.Compact(pInner, data);

                    // Store the squashed elements to the first output array.
                    // (This uses the loop predicate, so some additional zeros may be stored).
                    Sve.StoreAndZip(pLoop, left + indexLeft, compacted);

                    // Increment the position in the first output array by the number of elements found.
                    indexLeft = Sve.SaturatingIncrementByActiveElementCount(indexLeft, pInner);

                    // Find all elements in input array NOT less than the first element.
                    // (Flip the pCompare predicate by XORing with ones)
                    pInner = Sve.ConditionalSelect(pLoop, Sve.Xor(pCompare, ones), Vector<uint>.Zero);

                    // Repeat for the right array.
                    compacted = Sve.Compact(pInner, data);
                    Sve.StoreAndZip(pLoop, right + indexRight, compacted);
                    indexRight = Sve.SaturatingIncrementByActiveElementCount(indexRight, pInner);

                    i = Sve.SaturatingIncrementBy32BitElementCount(i, 1);
                    pLoop = Sve.CreateWhileLessThanMask32Bit(i, Size);
                }

                return indexRight;
            }
        }

        [Benchmark]
        public unsafe ulong SveTail()
        {
            fixed (uint* input = _input, left = _left, right = _right)
            {
                long i = 0;

                ulong indexLeft = 0;
                ulong indexRight = 0;

                Vector<uint> firstElemVec = Sve.DuplicateSelectedScalarToVector(
                    Sve.LoadVector(Sve.CreateTrueMaskUInt32(), input), 0
                );

                Vector<uint> pTrue = Sve.CreateTrueMaskUInt32();

                while (i < (Size - (int)Sve.Count32BitElements()))
                {
                    Vector<uint> data = Sve.LoadVector(pTrue, input + i);

                    // Predicate for elements in input array less than the first element.
                    Vector<uint> pInner = Sve.CompareLessThan(data, firstElemVec);

                    // Squash all found elements to the lower lanes of the vector.
                    Vector<uint> compacted = Sve.Compact(pInner, data);

                    // Store the squashed elements to the first output array.
                    Sve.StoreAndZip(pTrue, left + indexLeft, compacted);

                    // Increment the position in the first output array by the number of elements found.
                    indexLeft = Sve.SaturatingIncrementByActiveElementCount(indexLeft, pInner);

                    // Find elements greater than or equal to the first element.
                    pInner = Sve.CompareGreaterThanOrEqual(data, firstElemVec);

                    // Repeat for the right array.
                    compacted = Sve.Compact(pInner, data);
                    Sve.StoreAndZip(pTrue, right + indexRight, compacted);
                    indexRight = Sve.SaturatingIncrementByActiveElementCount(indexRight, pInner);

                    i = Sve.SaturatingIncrementBy32BitElementCount(i, 1);
                }

                // Handler remaining elements.
                for (; i < Size; i++)
                {
                    if (input[i] < input[0])
                    {
                        left[indexLeft] = input[i];
                        indexLeft++;
                    }
                    else
                    {
                        right[indexRight] = input[i];
                        indexRight++;
                    }
                }

                return indexRight;
            }
        }
    }
}
