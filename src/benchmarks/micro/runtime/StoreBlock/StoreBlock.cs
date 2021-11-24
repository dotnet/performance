// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

[BenchmarkCategory(Categories.Runtime, Categories.JIT)]
public class StoreBlock
{
    const int OperationsPerInvoke = 1000;

    byte[] _srcData = new byte[1024];
    byte[] _dstData = new byte[1024];

    [GlobalSetup]
    public void Setup()
    {
        for (int i = 0; i < _srcData.Length; i++)
            _srcData[i] = (byte)(i % 256);

        for (int i = 0; i < _dstData.Length; i++)
            _dstData[i] = (byte)(i % 256);
    }

    [StructLayout(LayoutKind.Explicit, Size=8)]
    struct Struct8
    {
    }

    Struct8 fld8;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap8()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 8);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr8()
    {
        Struct8 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 8);
        }

        fld8 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap8()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 8);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr8()
    {
        Struct8 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 8);
        }

        fld8 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap8()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 8);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr8()
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

    [StructLayout(LayoutKind.Explicit, Size=16)]
    struct Struct16
    {
    }

    Struct16 fld16;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap16()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 16);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr16()
    {
        Struct16 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 16);
        }

        fld16 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap16()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 16);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr16()
    {
        Struct16 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 16);
        }

        fld16 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap16()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 16);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr16()
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

    [StructLayout(LayoutKind.Explicit, Size=24)]
    struct Struct24
    {
    }

    Struct24 fld24;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap24()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 24);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr24()
    {
        Struct24 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 24);
        }

        fld24 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap24()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 24);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr24()
    {
        Struct24 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 24);
        }

        fld24 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap24()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 24);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr24()
    {
        Struct24 srcLcl;
        Struct24 dstLcl;

        srcLcl = fld24;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 24);
        }

        fld24 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=32)]
    struct Struct32
    {
    }

    Struct32 fld32;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap32()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 32);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr32()
    {
        Struct32 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 32);
        }

        fld32 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap32()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 32);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr32()
    {
        Struct32 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 32);
        }

        fld32 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap32()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 32);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr32()
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

    [StructLayout(LayoutKind.Explicit, Size=40)]
    struct Struct40
    {
    }

    Struct40 fld40;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap40()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 40);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr40()
    {
        Struct40 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 40);
        }

        fld40 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap40()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 40);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr40()
    {
        Struct40 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 40);
        }

        fld40 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap40()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 40);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr40()
    {
        Struct40 srcLcl;
        Struct40 dstLcl;

        srcLcl = fld40;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 40);
        }

        fld40 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=48)]
    struct Struct48
    {
    }

    Struct48 fld48;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap48()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 48);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr48()
    {
        Struct48 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 48);
        }

        fld48 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap48()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 48);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr48()
    {
        Struct48 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 48);
        }

        fld48 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap48()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 48);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr48()
    {
        Struct48 srcLcl;
        Struct48 dstLcl;

        srcLcl = fld48;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 48);
        }

        fld48 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=56)]
    struct Struct56
    {
    }

    Struct56 fld56;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap56()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 56);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr56()
    {
        Struct56 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 56);
        }

        fld56 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap56()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 56);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr56()
    {
        Struct56 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 56);
        }

        fld56 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap56()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 56);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr56()
    {
        Struct56 srcLcl;
        Struct56 dstLcl;

        srcLcl = fld56;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 56);
        }

        fld56 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=64)]
    struct Struct64
    {
    }

    Struct64 fld64;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap64()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 64);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr64()
    {
        Struct64 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 64);
        }

        fld64 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap64()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 64);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr64()
    {
        Struct64 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 64);
        }

        fld64 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap64()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 64);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr64()
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

    [StructLayout(LayoutKind.Explicit, Size=72)]
    struct Struct72
    {
    }

    Struct72 fld72;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap72()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 72);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr72()
    {
        Struct72 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 72);
        }

        fld72 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap72()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 72);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr72()
    {
        Struct72 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 72);
        }

        fld72 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap72()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 72);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr72()
    {
        Struct72 srcLcl;
        Struct72 dstLcl;

        srcLcl = fld72;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 72);
        }

        fld72 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=80)]
    struct Struct80
    {
    }

    Struct80 fld80;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap80()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 80);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr80()
    {
        Struct80 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 80);
        }

        fld80 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap80()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 80);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr80()
    {
        Struct80 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 80);
        }

        fld80 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap80()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 80);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr80()
    {
        Struct80 srcLcl;
        Struct80 dstLcl;

        srcLcl = fld80;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 80);
        }

        fld80 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=88)]
    struct Struct88
    {
    }

    Struct88 fld88;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap88()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 88);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr88()
    {
        Struct88 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 88);
        }

        fld88 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap88()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 88);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr88()
    {
        Struct88 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 88);
        }

        fld88 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap88()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 88);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr88()
    {
        Struct88 srcLcl;
        Struct88 dstLcl;

        srcLcl = fld88;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 88);
        }

        fld88 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=96)]
    struct Struct96
    {
    }

    Struct96 fld96;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap96()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 96);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr96()
    {
        Struct96 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 96);
        }

        fld96 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap96()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 96);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr96()
    {
        Struct96 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 96);
        }

        fld96 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap96()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 96);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr96()
    {
        Struct96 srcLcl;
        Struct96 dstLcl;

        srcLcl = fld96;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 96);
        }

        fld96 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=104)]
    struct Struct104
    {
    }

    Struct104 fld104;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap104()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 104);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr104()
    {
        Struct104 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 104);
        }

        fld104 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap104()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 104);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr104()
    {
        Struct104 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 104);
        }

        fld104 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap104()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 104);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr104()
    {
        Struct104 srcLcl;
        Struct104 dstLcl;

        srcLcl = fld104;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 104);
        }

        fld104 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=112)]
    struct Struct112
    {
    }

    Struct112 fld112;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap112()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 112);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr112()
    {
        Struct112 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 112);
        }

        fld112 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap112()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 112);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr112()
    {
        Struct112 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 112);
        }

        fld112 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap112()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 112);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr112()
    {
        Struct112 srcLcl;
        Struct112 dstLcl;

        srcLcl = fld112;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 112);
        }

        fld112 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=120)]
    struct Struct120
    {
    }

    Struct120 fld120;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap120()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 120);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr120()
    {
        Struct120 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 120);
        }

        fld120 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap120()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 120);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr120()
    {
        Struct120 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 120);
        }

        fld120 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap120()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 120);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr120()
    {
        Struct120 srcLcl;
        Struct120 dstLcl;

        srcLcl = fld120;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(&dstLcl, &srcLcl, 120);
        }

        fld120 = dstLcl;
    }

    [StructLayout(LayoutKind.Explicit, Size=128)]
    struct Struct128
    {
    }

    Struct128 fld128;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllZerosHeap128()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 0, 128);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllZerosLocalAddr128()
    {
        Struct128 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 0, 128);
        }

        fld128 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InitBlockAllOnesHeap128()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(ref _dstData[0], 255, 128);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void InitBlockAllOnesLocalAddr128()
    {
        Struct128 dstLcl;

        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.InitBlock(&dstLcl, 1, 128);
        }

        fld128 = dstLcl;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyBlockHeap128()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            Unsafe.CopyBlock(ref _dstData[0], ref _srcData[0], 128);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public unsafe void CopyBlockLocalAddr128()
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
