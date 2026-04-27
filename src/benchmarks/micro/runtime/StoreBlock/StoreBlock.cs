// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace StoreBlock
{
    [BenchmarkCategory(Categories.Runtime, Categories.JIT)]
    public class AnyLocation
    {
        const int Size = 4096;

        byte[] _srcData;
        byte[] _dstData;

        [GlobalSetup]
        public void Setup()
        {
            _srcData = ValuesGenerator.Array<byte>(Size);
            _dstData = ValuesGenerator.Array<byte>(Size);
        }

        [Benchmark(OperationsPerInvoke = Size / 8)]
        public void InitBlockAllZeros8()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 8)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 0, 8);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 8)]
        public void InitBlockAllOnes8()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 8)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 255, 8);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 8)]
        [MemoryRandomization]
        public void CopyBlock8()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 8)
            {
                Unsafe.CopyBlock(ref _dstData[startOffset], ref _srcData[startOffset], 8);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 16)]
        public void InitBlockAllZeros16()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 16)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 0, 16);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 16)]
        public void InitBlockAllOnes16()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 16)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 255, 16);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 16)]
        [MemoryRandomization]
        public void CopyBlock16()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 16)
            {
                Unsafe.CopyBlock(ref _dstData[startOffset], ref _srcData[startOffset], 16);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 32)]
        public void InitBlockAllZeros32()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 32)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 0, 32);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 32)]
        public void InitBlockAllOnes32()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 32)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 255, 32);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 32)]
        [MemoryRandomization]
        public void CopyBlock32()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 32)
            {
                Unsafe.CopyBlock(ref _dstData[startOffset], ref _srcData[startOffset], 32);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 64)]
        [MemoryRandomization]
        public void InitBlockAllZeros64()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 64)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 0, 64);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 64)]
        [MemoryRandomization]
        public void InitBlockAllOnes64()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 64)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 255, 64);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 64)]
        [MemoryRandomization]
        public void CopyBlock64()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 64)
            {
                Unsafe.CopyBlock(ref _dstData[startOffset], ref _srcData[startOffset], 64);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 128)]
        [MemoryRandomization]
        public void InitBlockAllZeros128()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 128)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 0, 128);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 128)]
        [MemoryRandomization]
        public void InitBlockAllOnes128()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 128)
            {
                Unsafe.InitBlock(ref _dstData[startOffset], 255, 128);
            }
        }

        [Benchmark(OperationsPerInvoke = Size / 128)]
        [MemoryRandomization]
        public void CopyBlock128()
        {
            for (int startOffset = 0; startOffset < Size; startOffset += 128)
            {
                Unsafe.CopyBlock(ref _dstData[startOffset], ref _srcData[startOffset], 128);
            }
        }
    }

    [BenchmarkCategory(Categories.Runtime, Categories.JIT)]
    public class LocalAddress
    {
        const int OperationsPerInvoke = 100;

        [StructLayout(LayoutKind.Explicit, Size=8)]
        struct Struct8
        {
        }

        Struct8 fld8;

        [StructLayout(LayoutKind.Explicit, Size=16)]
        struct Struct16
        {
        }

        Struct16 fld16;

        [StructLayout(LayoutKind.Explicit, Size=32)]
        struct Struct32
        {
        }

        Struct32 fld32;

        [StructLayout(LayoutKind.Explicit, Size=64)]
        struct Struct64
        {
        }

        Struct64 fld64;

        [StructLayout(LayoutKind.Explicit, Size=128)]
        struct Struct128
        {
        }

        Struct128 fld128;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void InitBlockAllZeros8()
        {
            Struct8 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 0, 8);
            }

            fld8 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void InitBlockAllOnes8()
        {
            Struct8 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 255, 8);
            }

            fld8 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void CopyBlock8()
        {
            Struct8 srcLcl;
            Struct8 dstLcl;

            srcLcl = fld8;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.CopyBlock(&dstLcl, &srcLcl, 8);
            }

            fld8 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void InitBlockAllZeros16()
        {
            Struct16 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 0, 16);
            }

            fld16 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MemoryRandomization]
        public unsafe void InitBlockAllOnes16()
        {
            Struct16 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 255, 16);
            }

            fld16 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void CopyBlock16()
        {
            Struct16 srcLcl;
            Struct16 dstLcl;

            srcLcl = fld16;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.CopyBlock(&dstLcl, &srcLcl, 16);
            }

            fld16 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void InitBlockAllZeros32()
        {
            Struct32 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 0, 32);
            }

            fld32 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MemoryRandomization]
        public unsafe void InitBlockAllOnes32()
        {
            Struct32 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 255, 32);
            }

            fld32 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void CopyBlock32()
        {
            Struct32 srcLcl;
            Struct32 dstLcl;

            srcLcl = fld32;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.CopyBlock(&dstLcl, &srcLcl, 32);
            }

            fld32 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void InitBlockAllZeros64()
        {
            Struct64 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 0, 64);
            }

            fld64 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MemoryRandomization]
        public unsafe void InitBlockAllOnes64()
        {
            Struct64 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 255, 64);
            }

            fld64 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void CopyBlock64()
        {
            Struct64 srcLcl;
            Struct64 dstLcl;

            srcLcl = fld64;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.CopyBlock(&dstLcl, &srcLcl, 64);
            }

            fld64 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MemoryRandomization]
        public unsafe void InitBlockAllZeros128()
        {
            Struct128 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 0, 128);
            }

            fld128 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [MemoryRandomization]
        public unsafe void InitBlockAllOnes128()
        {
            Struct128 dstLcl;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.InitBlock(&dstLcl, 255, 128);
            }

            fld128 = dstLcl;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public unsafe void CopyBlock128()
        {
            Struct128 srcLcl;
            Struct128 dstLcl;

            srcLcl = fld128;

            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                Unsafe.CopyBlock(&dstLcl, &srcLcl, 128);
            }

            fld128 = dstLcl;
        }
    }
}
