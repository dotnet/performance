using System;
using System.Numerics;
using System.Linq;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace SveBenchmarks
{
    [BenchmarkCategory(Categories.Runtime)]
    [OperatingSystemsArchitectureFilter(allowed: true, System.Runtime.InteropServices.Architecture.Arm64)]
    public class StrCmp
    {
        [Params(15, 127, 527, 10015)]
        public int Size;

        [Params("ChangeArr1", "ChangeArr2", "Zero")]
        public string Scenario;

        private byte[] _arr1, _arr2;

        [GlobalSetup]
        public virtual void Setup()
        {
            _arr1 = ValuesGenerator.Array<byte>(Size);
            _arr2 = ValuesGenerator.Array<byte>(Size);

            switch (Scenario)
            {
                case "ChangeArr1":
                    // modify arr1 value in the middle of the array
                    _arr1[Size / 2] = ValuesGenerator.GetNonDefaultValue<byte>();
                    break;

                case "ChangeArr2":
                    // modify arr2 value near the end of the array
                    _arr2[Size - 1] = ValuesGenerator.GetNonDefaultValue<byte>();
                    break;

                case "Zero":
                    // keep both arrays equal
                    break;
            }
        }

        [Benchmark]
        public int ScalarStrCmp()
        {
            if (_arr1.Length == _arr2.Length)
            {
                for (int i = 0; i < Size; i++)
                {
                    if (_arr1[i] != _arr2[i] )
                        return _arr1[i] - _arr2[i];
                }

                return 0;
            }

            Debug.Assert(false, "Different array lengths are not expected");
            return 0;
        }

        [Benchmark]
        public int Vector128StrCmp()
        {
            int incr = Vector128<byte>.Count;
            int i = 0;

            if (_arr1.Length == _arr2.Length)
            {
                for (; i <= Size - incr; i += incr)
                {
                    Vector128<byte> arr1_vals = Vector128.LoadUnsafe(ref _arr1[i]);
                    Vector128<byte> arr2_vals = Vector128.LoadUnsafe(ref _arr2[i]);

                    bool allEqual = Vector128.EqualsAll(arr1_vals, arr2_vals);

                    if (!allEqual)
                    {
                        break;
                    }
                }

                // fall back to scalar for remaining values
                for (; i < Size; i++)
                {
                    if (_arr1[i] != _arr2[i] )
                        return _arr1[i] - _arr2[i];
                }
                return 0;
            }

            Debug.Assert(false, "Different array lengths are not expected");
            return 0;
        }


        [Benchmark]
        public unsafe long SveStrCmp()
        {
            if (Sve.IsSupported)
            {
                int i = 0;
                int elemsInVector = (int)Sve.Count8BitElements();

                Vector<byte> ptrue = Sve.CreateTrueMaskByte();
                Vector<byte> pLoop = (Vector<byte>)Sve.CreateWhileLessThanMask8Bit(i, Size);
                Vector<byte> cmp = Vector<byte>.Zero;
                Vector<byte> arr1_data, arr2_data;

                if (_arr1.Length == _arr2.Length)
                {
                    fixed (byte* arr1_ptr = _arr1, arr2_ptr = _arr2)
                    {
                        while (Sve.TestFirstTrue(ptrue, pLoop))
                        {
                            arr1_data = Sve.LoadVector(pLoop, arr1_ptr + i);
                            arr2_data = Sve.LoadVector(pLoop, arr2_ptr + i);

                            // stop if any values arent equal
                            cmp = Sve.CompareNotEqualTo(arr1_data, arr2_data);

                            if (Sve.TestAnyTrue(ptrue, cmp))
                                break;

                            i += elemsInVector;

                            pLoop = (Vector<byte>)Sve.CreateWhileLessThanMask8Bit(i, Size);
                        }

                        // create a bitmask to find position of changed value
                        int mask = 0;
                        for (int j = 0; j < elemsInVector; j++)
                        {
                            // set bits in lanes with non zero elements
                            if (cmp.GetElement(j) != 0)
                                mask |= (1 << j);
                        }

                        int zeroCount = BitOperations.TrailingZeroCount(mask);

                        if (zeroCount < elemsInVector)
                            return _arr1[i+zeroCount] - _arr2[i+zeroCount];

                        return 0;
                    }
                }

                Debug.Assert(false, "Different array lengths are not expected");
                return 0;
            }
            return 0;
        }

        [Benchmark]
        public unsafe long SveStrCmpTail()
        {
            if (Sve.IsSupported)
            {
                Vector<byte> ptrue = Sve.CreateTrueMaskByte();
                Vector<byte> cmp;
                Vector<byte> arr1_data, arr2_data;

                int i = 0;
                int elemsInVector = (int)Sve.Count8BitElements();

                if (_arr1.Length == _arr2.Length)
                {
                    fixed (byte* arr1_ptr = _arr1, arr2_ptr = _arr2)
                    {
                        for (; i <= Size - elemsInVector; i += elemsInVector)
                        {
                            arr1_data = Sve.LoadVector(ptrue, arr1_ptr + i);
                            arr2_data = Sve.LoadVector(ptrue, arr2_ptr + i);

                            cmp = Sve.CompareNotEqualTo(arr1_data, arr2_data);

                            byte allEqual = (byte)Sve.AddAcross(cmp).ToScalar();

                            if (allEqual > 0)
                            {
                                break;
                            }
                        }

                        for (; i < Size; i++)
                        {
                            if (_arr1[i] != _arr2[i] )
                                return _arr1[i] - _arr2[i];
                        }

                        return 0;
                    }
                }

                Debug.Assert(false, "Different array lengths are not expected");
                return 0;
            }

            return 0;
        }
    }
}