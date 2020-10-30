// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Runtime.InteropServices;

namespace Interop
{
    [BenchmarkCategory(Categories.Runtime)]
    public class StructureToPtr
    {
        [Benchmark]
        public unsafe int MarshalStructureToPtr()
        {
            NonBlittableStruct str = new NonBlittableStruct
            {
                i = 1,
                s = 42,
                b = true
            };

            byte* stackSpace = stackalloc byte[Marshal.SizeOf<NonBlittableStruct>()];
            Marshal.StructureToPtr(str, (IntPtr)stackSpace, false);
            return *(int*)stackSpace;
        }

        [Benchmark]
        public unsafe NonBlittableStruct MarshalPtrToStructure()
        {
            byte* stackSpace = stackalloc byte[Marshal.SizeOf<NonBlittableStruct>()];

            *(int*)stackSpace = 1;
            *(short*)(stackSpace + sizeof(int)) = 42;
            *(byte*)(stackSpace + sizeof(int) + sizeof(short)) = 1;

            return Marshal.PtrToStructure<NonBlittableStruct>((IntPtr)stackSpace);
        }

        [Benchmark]
        public unsafe int MarshalDestroyStructure()
        {
            NonBlittableAllocatingStruct str = new NonBlittableAllocatingStruct
            {
                i = 42,
                s = "Hello World!"
            };

            byte* stackSpace = stackalloc byte[Marshal.SizeOf<NonBlittableAllocatingStruct>()];

            Marshal.StructureToPtr(str, (IntPtr)stackSpace, false);

            int returnValue = *(int*)stackSpace;

            Marshal.DestroyStructure<NonBlittableAllocatingStruct>((IntPtr)stackSpace);

            return returnValue;
        }

        public struct NonBlittableStruct
        {
            public int i;
            public short s;
            [MarshalAs(UnmanagedType.U1)]
            public bool b;
        }

        public struct NonBlittableAllocatingStruct
        {
            public int i;
            [MarshalAs(UnmanagedType.LPStr)]
            public string s;
        }
    }
}
