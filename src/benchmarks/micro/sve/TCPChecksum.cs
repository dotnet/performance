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
    public class TCPChecksum
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddFilter(new SimpleFilter(_ => Sve.IsSupported));
            }
        }

        [Params(527, 1234, 10015)]
        public int Size;

        private byte[] _packets;
        private int _psize;
        private uint _result;

        [GlobalSetup]
        public virtual unsafe void Setup()
        {
            Random rand = new Random(0);
            _packets = ValuesGenerator.Array<byte>(Size);
            fixed (byte* p = _packets)
            {
                int start = 0;
                // Generate random packet lengths in the range [55, 255).
                int end = ((int)rand.Next() % 200) + 55;
                while (end < Size)
                {
                    // Store the length as misaligned 16-bit at offset 1 of each packet
                    ushort* plength = (ushort*)(p + start + 1);
                    *plength = (ushort)(end - start);

                    start = end;
                    end = start + ((int)rand.Next() % 200) + 55;
                }
                // Save the total size of all the packets.
                _psize = start;
            }
        }

        [GlobalCleanup]
        public virtual void Verify()
        {
            uint current = _result;
            Setup();
            Scalar();
            uint scalar = _result;
            // Check that the result is the same as the scalar result.
            Debug.Assert(current == scalar);
        }

        // The following algorithms are adapted from the Arm simd-loops repository:
        // https://gitlab.arm.com/architecture/simd-loops/-/blob/main/loops/loop_022.c

        [Benchmark]
        public unsafe void Scalar()
        {
            uint res = 0;
            fixed (byte* packets = _packets)
            {
                byte* p = packets;
                byte* lmt = p + _psize;

                while (p < lmt)
                {
                    // Read packet length from offset 1 and mask off the last bit.
                    ushort* plength = (ushort*)(p + 1);
                    ushort length = (ushort)(*plength & 0xfe);

                    // Sum up packet in chunks of 16-bit values.
                    ulong sum = 0;
                    byte* pLast = p + length;
                    for (ushort* i = (ushort*)p; i < pLast; i++)
                    {
                        ushort d = *i;
                        sum += d;
                    }
                    // Fold the overflow bits to the lower bits and add to the result.
                    // Then take the one's complement as the checksum.
                    // Only need one fold since the range of the sum will not exceed 32-bit.
                    ushort checksum = (ushort)(~((sum & 0xffff) + (sum >> 16)));

                    // Increment the packet pointer by its length.
                    p += *plength;

                    // Gather the count and checksum by XORing the results.
                    res += 1;
                    res ^= (uint)checksum << 16;
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void Vector128TCPChecksum()
        {
            uint res = 0;
            fixed (byte* packets = _packets)
            {
                byte* p = packets;
                byte* lmt = p + _psize;

                while (p < lmt)
                {
                    ushort* plength = (ushort*)(p + 1);
                    ushort length = (ushort)(*plength & 0xfe);

                    Vector128<uint> sum_l = Vector128<uint>.Zero;
                    Vector128<uint> sum_h = Vector128<uint>.Zero;
                    int i = 0;
                    for (; i <= length - 16; i += 16)
                    {
                        Vector128<ushort> d = AdvSimd.LoadVector128((ushort*)(p + i));
                        // Widen lower and upper halves from 16-bit to 32-bit and add to sum.
                        sum_l = AdvSimd.AddWideningLower(sum_l, d.GetLower());
                        sum_h = AdvSimd.AddWideningUpper(sum_h, d);
                    }

                    // Handle the remaining packet using 64-bit vectors.
                    for (; i <= length - 8; i += 8)
                    {
                        Vector64<ushort> d = AdvSimd.LoadVector64((ushort*)(p + i));
                        sum_l = AdvSimd.AddWideningLower(sum_l, d);
                    }

                    // Handle the remaining packet in scalar.
                    ulong sum = AdvSimd.Arm64.AddAcross(AdvSimd.Add(sum_l, sum_h)).ToScalar();
                    for (; i < length; i += 2)
                    {
                        ushort d = *((ushort*)(p + i));
                        sum += d;
                    }
                    ushort checksum = (ushort)(~((sum & 0xffff) + (sum >> 16)));

                    p += *plength;
                    res += 1;
                    res ^= (uint)checksum << 16;
                }
                _result = res;
            }
        }

        [Benchmark]
        public unsafe void SveTCPChecksum()
        {
            uint res = 0;
            fixed (byte* packets = _packets)
            {
                byte* p = packets;
                byte* lmt = p + _psize;

                Vector<ushort> ones = Vector<ushort>.One;
                Vector<byte> pTrue = Sve.CreateTrueMaskByte();

                while (p < lmt)
                {
                    ushort* plength = (ushort*)(p + 1);
                    ushort length = (ushort)(*plength & 0xfe);

                    int i = 0;
                    Vector<ulong> acc = Vector<ulong>.Zero;
                    Vector<byte> pLoop = Sve.CreateWhileLessThanMask8Bit(0, length);
                    while (Sve.TestAnyTrue(pTrue, pLoop))
                    {
                        Vector<ushort> d = (Vector<ushort>)Sve.LoadVector(pLoop, p + i);
                        // Compute dot product of the data and a vector of 1.
                        // The result is widened to 64-bit.
                        acc = Sve.DotProduct(acc, d, ones);

                        // Handle loop predicate.
                        i += (int)Sve.Count8BitElements();
                        pLoop = Sve.CreateWhileLessThanMask8Bit(i, length);
                    }
                    // Reduce result to scalar.
                    ulong sum = Sve.AddAcross(acc).ToScalar();
                    ushort checksum = (ushort)(~((sum & 0xffff) + (sum >> 16)));

                    p += *plength;
                    res += 1;
                    res ^= (uint)checksum << 16;
                }
                _result = res;
            }
        }

    }
}
