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
    public class StrLen
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

        private byte[] _array;
        private ulong _length;

        [GlobalSetup]
        public virtual void Setup()
        {
            _array = ValuesGenerator.Array<byte>(Size + 1);
            _length = 0;

            var random = new Random();
            for (int i=0; i < _array.Length; i++)
            {
                // Replaces any zero elements with a random value
                if (_array[i] == 0)
                {
                    _array[i] = (byte)random.Next(1, byte.MaxValue);
                }
            }

            _array[Size] = 0; // add zero to the end to simulate a terminated string

        }

        [Benchmark]
        public unsafe ulong ScalarStrLen()
        {
            fixed (byte* arr_ptr = _array)
            {
                if (arr_ptr == null)
                    return 0;

                byte* ptr = arr_ptr;

                while (*ptr != 0)
                {
                    _length++;
                    ptr++;
                }
            }

            return _length;
        }

        [Benchmark]
        public unsafe ulong Vector128StrLen()
        {
            Vector128<byte> data = Vector128<byte>.Zero;
            ulong cmp = 0;
            ulong i = 0;
            ulong alignOffset = 0;

            fixed(byte* ptr = _array)
            {
                byte* arr_ptr = ptr;

                // Check for a zero in first 16 bytes
                for (i = 0; i < 16; i++)
                {
                    if(arr_ptr[i] == 0)
                    {
                        return i;
                    }
                }

                // look for a zero in the next 16 byte block
                while (cmp == 0)
                {
                    data = Vector128.Load(arr_ptr + i);
                    Vector128<byte> min = AdvSimd.Arm64.MinPairwise(data, data);
                    Vector64<byte> cmpVec = Vector64.Equals(min.GetLower(), Vector64<byte>.Zero);

                    cmp = cmpVec.AsUInt64().ToScalar();

                    i = i + (ulong)(sizeof(Vector128<byte>) / sizeof(byte));
                }

                // once a zero is found, go back one 16-byte block and find location of the zero
                i = i - (ulong)(sizeof(Vector128<byte>) / sizeof(byte));

                Vector128<byte> cmpVecLoc = AdvSimd.CompareEqual(data, Vector128<byte>.Zero);

                Vector64<byte> shifted = AdvSimd.ShiftRightLogicalNarrowingLower(
                    cmpVecLoc.AsUInt16(),
                    4
                );

                ulong syncd = shifted.AsUInt64().ToScalar();
                int count = BitOperations.TrailingZeroCount(syncd);

                return i + (ulong)(count / 4) + alignOffset;
            }
        }

        [Benchmark]
        public unsafe ulong SveStrLen()
        {
            if (Sve.IsSupported)
            {
                Vector<byte> ptrue = Sve.CreateTrueMaskByte();
                Vector<byte> cmp, data;

                ulong i = 0;
                ulong elemsInVector = Sve.Count8BitElements();

                Vector<byte> pLoop = (Vector<byte>)Sve.CreateWhileLessThanMask8Bit((int)i, Size);

                fixed (byte* arr_ptr = _array)
                {
                    while (true)
                    {
                        data = Sve.LoadVector(pLoop, arr_ptr + i);
                        cmp = Sve.CompareEqual(data, Vector<byte>.Zero);

                        if (Sve.TestAnyTrue(ptrue, cmp))
                            break;
                        else
                        {
                            i += elemsInVector;
                            pLoop = (Vector<byte>)Sve.CreateWhileLessThanMask8Bit((int)i, Size);
                        }
                    }

                    i += Sve.GetActiveElementCount(pLoop, data);
                    return i;
                }
            }
            return 0;
        }
    }
}