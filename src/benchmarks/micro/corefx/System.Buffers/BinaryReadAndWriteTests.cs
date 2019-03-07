// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MicroBenchmarks;

using static System.Buffers.Binary.BinaryPrimitives;

namespace System.Buffers.Binary.Tests
{
    [BenchmarkCategory(Categories.CoreFX, Categories.CoreCLR)]
    public class BinaryReadAndWriteTests
    {
        private readonly static byte[] _arrayLE = GetSpanLE().ToArray();
        private readonly static byte[] _arrayBE = GetSpanBE().ToArray();
        private readonly static int[] _oneThousandIntegers = new int[1000];

        [Benchmark]
        public TestStructExplicit ReadStructAndReverseBE()
        {
            Span<byte> spanBE = new Span<byte>(_arrayBE);

            var readStruct = MemoryMarshal.Read<TestStructExplicit>(spanBE);
            if (BitConverter.IsLittleEndian)
            {
                readStruct.S0 = ReverseEndianness(readStruct.S0);
                readStruct.I0 = ReverseEndianness(readStruct.I0);
                readStruct.L0 = ReverseEndianness(readStruct.L0);
                readStruct.US0 = ReverseEndianness(readStruct.US0);
                readStruct.UI0 = ReverseEndianness(readStruct.UI0);
                readStruct.UL0 = ReverseEndianness(readStruct.UL0);
                readStruct.S1 = ReverseEndianness(readStruct.S1);
                readStruct.I1 = ReverseEndianness(readStruct.I1);
                readStruct.L1 = ReverseEndianness(readStruct.L1);
                readStruct.US1 = ReverseEndianness(readStruct.US1);
                readStruct.UI1 = ReverseEndianness(readStruct.UI1);
                readStruct.UL1 = ReverseEndianness(readStruct.UL1);
            }

            return readStruct;
        }

        [Benchmark]
        public TestStructExplicit ReadStructAndReverseLE()
        {
            Span<byte> spanLE = new Span<byte>(_arrayLE);

            var readStruct = MemoryMarshal.Read<TestStructExplicit>(spanLE);
            if (!BitConverter.IsLittleEndian)
            {
                readStruct.S0 = ReverseEndianness(readStruct.S0);
                readStruct.I0 = ReverseEndianness(readStruct.I0);
                readStruct.L0 = ReverseEndianness(readStruct.L0);
                readStruct.US0 = ReverseEndianness(readStruct.US0);
                readStruct.UI0 = ReverseEndianness(readStruct.UI0);
                readStruct.UL0 = ReverseEndianness(readStruct.UL0);
                readStruct.S1 = ReverseEndianness(readStruct.S1);
                readStruct.I1 = ReverseEndianness(readStruct.I1);
                readStruct.L1 = ReverseEndianness(readStruct.L1);
                readStruct.US1 = ReverseEndianness(readStruct.US1);
                readStruct.UI1 = ReverseEndianness(readStruct.UI1);
                readStruct.UL1 = ReverseEndianness(readStruct.UL1);
            }

            return readStruct;
        }

        [Benchmark]
        public TestStructExplicit ReadStructFieldByFieldBE()
        {
            Span<byte> spanBE = new Span<byte>(_arrayBE);

            var readStruct = new TestStructExplicit
            {
                S0 = ReadInt16BigEndian(spanBE),
                I0 = ReadInt32BigEndian(spanBE.Slice(2)),
                L0 = ReadInt64BigEndian(spanBE.Slice(6)),
                US0 = ReadUInt16BigEndian(spanBE.Slice(14)),
                UI0 = ReadUInt32BigEndian(spanBE.Slice(16)),
                UL0 = ReadUInt64BigEndian(spanBE.Slice(20)),
                S1 = ReadInt16BigEndian(spanBE.Slice(28)),
                I1 = ReadInt32BigEndian(spanBE.Slice(30)),
                L1 = ReadInt64BigEndian(spanBE.Slice(34)),
                US1 = ReadUInt16BigEndian(spanBE.Slice(42)),
                UI1 = ReadUInt32BigEndian(spanBE.Slice(44)),
                UL1 = ReadUInt64BigEndian(spanBE.Slice(48))
            };

            return readStruct;
        }

        [Benchmark]
        public TestStructExplicit ReadStructFieldByFieldLE()
        {
            Span<byte> spanLE = new Span<byte>(_arrayLE);

            var readStruct = new TestStructExplicit
            {
                S0 = ReadInt16LittleEndian(spanLE),
                I0 = ReadInt32LittleEndian(spanLE.Slice(2)),
                L0 = ReadInt64LittleEndian(spanLE.Slice(6)),
                US0 = ReadUInt16LittleEndian(spanLE.Slice(14)),
                UI0 = ReadUInt32LittleEndian(spanLE.Slice(16)),
                UL0 = ReadUInt64LittleEndian(spanLE.Slice(20)),
                S1 = ReadInt16LittleEndian(spanLE.Slice(28)),
                I1 = ReadInt32LittleEndian(spanLE.Slice(30)),
                L1 = ReadInt64LittleEndian(spanLE.Slice(34)),
                US1 = ReadUInt16LittleEndian(spanLE.Slice(42)),
                UI1 = ReadUInt32LittleEndian(spanLE.Slice(44)),
                UL1 = ReadUInt64LittleEndian(spanLE.Slice(48))
            };

            return readStruct;
        }

        [Benchmark]
        public TestStructExplicit ReadStructFieldByFieldUsingBitConverterLE()
        {
            byte[] arrayLE = _arrayLE;

            var readStruct = new TestStructExplicit
            {
                S0 = BitConverter.ToInt16(arrayLE, 0),
                I0 = BitConverter.ToInt32(arrayLE, 2),
                L0 = BitConverter.ToInt64(arrayLE, 6),
                US0 = BitConverter.ToUInt16(arrayLE, 14),
                UI0 = BitConverter.ToUInt32(arrayLE, 16),
                UL0 = BitConverter.ToUInt64(arrayLE, 20),
                S1 = BitConverter.ToInt16(arrayLE, 28),
                I1 = BitConverter.ToInt32(arrayLE, 30),
                L1 = BitConverter.ToInt64(arrayLE, 34),
                US1 = BitConverter.ToUInt16(arrayLE, 42),
                UI1 = BitConverter.ToUInt32(arrayLE, 44),
                UL1 = BitConverter.ToUInt64(arrayLE, 48),
            };

            return readStruct;
        }

        [Benchmark]
        public TestStructExplicit ReadStructFieldByFieldUsingBitConverterBE()
        {
            byte[] arrayBE = _arrayBE;

            var readStruct = new TestStructExplicit
            {
                S0 = BitConverter.ToInt16(arrayBE, 0),
                I0 = BitConverter.ToInt32(arrayBE, 2),
                L0 = BitConverter.ToInt64(arrayBE, 6),
                US0 = BitConverter.ToUInt16(arrayBE, 14),
                UI0 = BitConverter.ToUInt32(arrayBE, 16),
                UL0 = BitConverter.ToUInt64(arrayBE, 20),
                S1 = BitConverter.ToInt16(arrayBE, 28),
                I1 = BitConverter.ToInt32(arrayBE, 30),
                L1 = BitConverter.ToInt64(arrayBE, 34),
                US1 = BitConverter.ToUInt16(arrayBE, 42),
                UI1 = BitConverter.ToUInt32(arrayBE, 44),
                UL1 = BitConverter.ToUInt64(arrayBE, 48),
            };

            if (BitConverter.IsLittleEndian)
            {
                readStruct.S0 = ReverseEndianness(readStruct.S0);
                readStruct.I0 = ReverseEndianness(readStruct.I0);
                readStruct.L0 = ReverseEndianness(readStruct.L0);
                readStruct.US0 = ReverseEndianness(readStruct.US0);
                readStruct.UI0 = ReverseEndianness(readStruct.UI0);
                readStruct.UL0 = ReverseEndianness(readStruct.UL0);
                readStruct.S1 = ReverseEndianness(readStruct.S1);
                readStruct.I1 = ReverseEndianness(readStruct.I1);
                readStruct.L1 = ReverseEndianness(readStruct.L1);
                readStruct.US1 = ReverseEndianness(readStruct.US1);
                readStruct.UI1 = ReverseEndianness(readStruct.UI1);
                readStruct.UL1 = ReverseEndianness(readStruct.UL1);
            }

            return readStruct;
        }

        [Benchmark]
        public int[] MeasureReverseEndianness()
        {
            var local = _oneThousandIntegers;

            for (int j = 0; j < local.Length; j++)
            {
                local[j] = ReverseEndianness(local[j]);
            }

            return local;
        }

        [Benchmark]
        public int[] MeasureReverseUsingNtoH()
        {
            var local = _oneThousandIntegers;

            for (int j = 0; j < local.Length; j++)
            {
                local[j] = IPAddress.NetworkToHostOrder(local[j]);
            }

            return local;

        }

        private static Span<byte> GetSpanBE()
        {
            Span<byte> spanBE = new byte[Unsafe.SizeOf<TestStructExplicit>()];

            TestStructExplicit testExplicitStruct = CreateTestExplicitStruct();

            WriteInt16BigEndian(spanBE, testExplicitStruct.S0);
            WriteInt32BigEndian(spanBE.Slice(2), testExplicitStruct.I0);
            WriteInt64BigEndian(spanBE.Slice(6), testExplicitStruct.L0);
            WriteUInt16BigEndian(spanBE.Slice(14), testExplicitStruct.US0);
            WriteUInt32BigEndian(spanBE.Slice(16), testExplicitStruct.UI0);
            WriteUInt64BigEndian(spanBE.Slice(20), testExplicitStruct.UL0);
            WriteInt16BigEndian(spanBE.Slice(28), testExplicitStruct.S1);
            WriteInt32BigEndian(spanBE.Slice(30), testExplicitStruct.I1);
            WriteInt64BigEndian(spanBE.Slice(34), testExplicitStruct.L1);
            WriteUInt16BigEndian(spanBE.Slice(42), testExplicitStruct.US1);
            WriteUInt32BigEndian(spanBE.Slice(44), testExplicitStruct.UI1);
            WriteUInt64BigEndian(spanBE.Slice(48), testExplicitStruct.UL1);

            return spanBE;
        }

        private static Span<byte> GetSpanLE()
        {
            Span<byte> spanLE = new byte[Unsafe.SizeOf<TestStructExplicit>()];

            TestStructExplicit testExplicitStruct = CreateTestExplicitStruct();

            WriteInt16LittleEndian(spanLE, testExplicitStruct.S0);
            WriteInt32LittleEndian(spanLE.Slice(2), testExplicitStruct.I0);
            WriteInt64LittleEndian(spanLE.Slice(6), testExplicitStruct.L0);
            WriteUInt16LittleEndian(spanLE.Slice(14), testExplicitStruct.US0);
            WriteUInt32LittleEndian(spanLE.Slice(16), testExplicitStruct.UI0);
            WriteUInt64LittleEndian(spanLE.Slice(20), testExplicitStruct.UL0);
            WriteInt16LittleEndian(spanLE.Slice(28), testExplicitStruct.S1);
            WriteInt32LittleEndian(spanLE.Slice(30), testExplicitStruct.I1);
            WriteInt64LittleEndian(spanLE.Slice(34), testExplicitStruct.L1);
            WriteUInt16LittleEndian(spanLE.Slice(42), testExplicitStruct.US1);
            WriteUInt32LittleEndian(spanLE.Slice(44), testExplicitStruct.UI1);
            WriteUInt64LittleEndian(spanLE.Slice(48), testExplicitStruct.UL1);

            return spanLE;
        }

        private static TestStructExplicit CreateTestExplicitStruct() => new TestStructExplicit
        {
            S0 = short.MaxValue,
            I0 = int.MaxValue,
            L0 = long.MaxValue,
            US0 = ushort.MaxValue,
            UI0 = uint.MaxValue,
            UL0 = ulong.MaxValue,
            S1 = short.MinValue,
            I1 = int.MinValue,
            L1 = long.MinValue,
            US1 = ushort.MinValue,
            UI1 = uint.MinValue,
            UL1 = ulong.MinValue
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct TestStructExplicit
        {
            [FieldOffset(0)]
            public short S0;
            [FieldOffset(2)]
            public int I0;
            [FieldOffset(6)]
            public long L0;
            [FieldOffset(14)]
            public ushort US0;
            [FieldOffset(16)]
            public uint UI0;
            [FieldOffset(20)]
            public ulong UL0;
            [FieldOffset(28)]
            public short S1;
            [FieldOffset(30)]
            public int I1;
            [FieldOffset(34)]
            public long L1;
            [FieldOffset(42)]
            public ushort US1;
            [FieldOffset(44)]
            public uint UI1;
            [FieldOffset(48)]
            public ulong UL1;
        }
    }
}
