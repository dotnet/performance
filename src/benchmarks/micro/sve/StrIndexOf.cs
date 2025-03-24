using System;
using System.Numerics;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace SveBenchmarks
{
    [BenchmarkCategory(Categories.Runtime)]
    public class StrIndexOf
    {
        [Params(15, 127, 527, 10015)]
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
        public int ScalarIndexOf()
        {
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i] == _searchValue)
                {
                    return i;
                }
            }
            return -1;
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

                    ushort cmpSum = Vector128.Sum<ushort>(cmp);

                    if (cmpSum > 0)
                    {
                        // find index of matching item
                        for (int j = 0; j < incr; j++)
                        {
                            if (cmp.GetElement(j) == ushort.MaxValue)
                            {
                                return i + j;
                            }
                        }
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

                for (; Sve.TestFirstTrue(Sve.CreateTrueMaskUInt16(), pLoop);  i += (int)Sve.Count16BitElements())
                {
                    Vector<ushort> vals = Sve.LoadVector(pLoop, ((ushort*)arr_ptr) + i);
                    Vector<ushort> cmpVec = Sve.CompareEqual(vals, target);

                    ushort cmpSum = (ushort)Sve.AddAcross(cmpVec).ToScalar();

                    if (cmpSum > 0)
                    {
                        // find index of matching item
                        for (int j = 0; j < Vector<ushort>.Count; j++)
                        {
                            if (cmpVec.GetElement(j) == 1)
                            {
                                return i + j;
                            }
                        }
                    }

                    pLoop = (Vector<ushort>)Sve.CreateWhileLessThanMask16Bit(i, Size);
                }
            }

            return -1;

        }

        [Benchmark]
        public unsafe int SveIndexOfTail()
        {
            int i = 0;

            fixed (char* arr_ptr = _array)
            {
                Vector<ushort> target = new Vector<ushort>((ushort)_searchValue);
                var pLoop = (Vector<ushort>)Sve.CreateTrueMaskInt16();


                for (; (Size - i) > (int)Sve.Count16BitElements(); i += (int)Sve.Count16BitElements())
                {
                    Vector<ushort> vals = Sve.LoadVector(pLoop, ((ushort*)arr_ptr) + i);
                    Vector<ushort> cmpVec = Sve.CompareEqual(vals, target);

                    ushort cmpSum = (ushort)Sve.AddAcross(cmpVec).ToScalar();

                    if (cmpSum > 0)
                    {
                        // find index of matching item
                        for (int j = 0; j < Vector<ushort>.Count; j++)
                        {
                            if (cmpVec.GetElement(j) == 1)
                            {
                                return i + j;
                            }
                        }
                    }
                }

                for (; i < Size; i++)
                {
                    if (_array[i] == _searchValue)
                        return i;
                }

                return -1;
            }
        }

    }
}