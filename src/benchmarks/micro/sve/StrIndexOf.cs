using System;
using System.Numerics;
using System.Linq;
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
    public class StrIndexOf
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddFilter(new SimpleFilter(_ => Sve.IsSupported));
            }
        }

        [Params(19, 127, 527, 10015)]
        public int Size;

        private char[] _array;
        private char _searchValue;

        [GlobalSetup]
        public virtual void Setup()
        {
            _array = Enumerable.Range(1, Size)
                                .Select(i => (char) i)
                                .ToArray();
            _searchValue = _array[Size / 2];
        }

        [Benchmark]
        public unsafe int Scalar()
        {
            fixed (char* arr = _array)
            {
                for (int i = 0; i < Size; i++)
                {
                    if (arr[i] == _searchValue)
                    {
                        return i;
                    }
                }
                return -1;
            }
        }

        [Benchmark]
        public unsafe int Vector128IndexOf()
        {
            int incr = Vector128<ushort>.Count;
            int i = 0;


            fixed (char* arr_ptr = _array)
            {
                Vector128<ushort> target = Vector128.Create((ushort)_searchValue);

                for (; i <= Size - incr; i += incr)
                {
                    Vector128<ushort> vals = Vector128.Load(((ushort*)arr_ptr) + i);

                    // Compare each vector value with the target
                    Vector128<ushort> cmp = Vector128.Equals(vals, target);

                    // Check if there is any match in vals by doing a pairwise maximum.
                    // The cmpMax UInt64 value will be non-zero if the character is found.
                    ulong cmpMax = AdvSimd.Arm64.MaxPairwise(cmp, cmp).AsUInt64().ToScalar();

                    if (cmpMax != 0)
                    {
                        // Convert to byte vector and extract the odd bytes into a 64-bit scalar.
                        Vector128<byte> cmpByte = cmp.AsByte();
                        ulong cmpUnzip = AdvSimd.Arm64.UnzipOdd(cmpByte, cmpByte).AsUInt64().ToScalar();

                        // Offset is the number of trailing bits (little endian) divided by 8.
                        int offset = BitOperations.TrailingZeroCount(cmpUnzip) >> 3;

                        return i + offset;
                    }
                }

                // Search the remaining values
                for (; i < Size; i++)
                {
                    if (_array[i] == _searchValue)
                        return i;
                }

                return -1;
            }
        }

        [Benchmark]
        public unsafe int SveIndexOf()
        {
            int i = 0;

            fixed (char* arr_ptr = _array)
            {
                Vector<ushort> target = new Vector<ushort>((ushort)_searchValue);
                var pLoop = (Vector<ushort>)Sve.CreateWhileLessThanMask16Bit(i, Size);

                while (Sve.TestFirstTrue(Sve.CreateTrueMaskUInt16(), pLoop))
                {
                    Vector<ushort> vals = Sve.LoadVector(pLoop, ((ushort*)arr_ptr) + i);
                    Vector<ushort> cmpVec = Sve.CompareEqual(vals, target);

                    // Test if the character is found in the current values.
                    if (Sve.TestAnyTrue(Sve.CreateTrueMaskUInt16(), cmpVec))
                    {
                        // Set elements up to and including the first active element to 1 and the rest to 0.
                        Vector<ushort> brkVec = Sve.CreateBreakAfterMask(Sve.CreateTrueMaskUInt16(), cmpVec);
                        // The offset is the number of active elements minus 1.
                        return (int)Sve.SaturatingIncrementByActiveElementCount(i - 1, brkVec);
                    }

                    i += (int)Sve.Count16BitElements();
                    pLoop = (Vector<ushort>)Sve.CreateWhileLessThanMask16Bit(i, Size);
                }

                return -1;
            }
        }

        [Benchmark]
        public unsafe int SveTail()
        {
            int i = 0;

            fixed (char* arr_ptr = _array)
            {
                Vector<ushort> target = new Vector<ushort>((ushort)_searchValue);
                var pLoop = (Vector<ushort>)Sve.CreateTrueMaskInt16();

                while (i < (Size - (int)Sve.Count16BitElements()))
                {
                    Vector<ushort> vals = Sve.LoadVector(pLoop, ((ushort*)arr_ptr) + i);
                    Vector<ushort> cmpVec = Sve.CompareEqual(vals, target);

                    // Test if the character is found in the current values.
                    if (Sve.TestAnyTrue(Sve.CreateTrueMaskUInt16(), cmpVec))
                    {
                        // Set elements up to and including the first active element to 1 and the rest to 0.
                        Vector<ushort> brkVec = Sve.CreateBreakAfterMask(Sve.CreateTrueMaskUInt16(), cmpVec);
                        // The offset is the number of active elements minus 1.
                        return (int)Sve.SaturatingIncrementByActiveElementCount(i - 1, brkVec);
                    }

                    i += (int)Sve.Count16BitElements();
                }

                for (; i < Size; i++)
                {
                    if (arr_ptr[i] == _searchValue)
                        return i;
                }

                return -1;
            }
        }

    }
}
