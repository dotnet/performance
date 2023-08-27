// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Interop
{
    [BenchmarkCategory(Categories.Runtime)]
    public unsafe class ComWrappersTests
    {
        static readonly ComWrappers s_instance = new DummyComWrappers();

        static object QueryRCW(nint ptr)
            => s_instance.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.None);

        private List<nint> iterations;
        private object targetRcw;

        public ComWrappersTests()
        {
            // Allocate a simple IUnknown instance.
            var ptr = (void**)NativeMemory.Alloc(5, (nuint)sizeof(void*));

            ptr[1] = (void*)1; // Initial ref count.

            // Define IUnknown vtable.
            ptr[2] = (delegate* unmanaged<void*, Guid*, void**, int>)&QueryInterface;
            ptr[3] = (delegate* unmanaged<void*, int>)&AddRef;
            ptr[4] = (delegate* unmanaged<void*, int>)&Release;

            // Assign vtable to instance.
            ptr[0] = &ptr[2];

            // Record RCW pointer.
            nint rcwPtr = (nint)ptr;

            // Transfer ownership to RCW instance. This populates the RCW cache.
            targetRcw = s_instance.GetOrCreateObjectForComInstance((nint)ptr, CreateObjectFlags.None);

            // Ownership was transferred.
            Marshal.Release((nint)ptr);

            // Define a large number of iterations for parallel action.
            var limit = 3_000_000;
            iterations = Enumerable.Repeat(rcwPtr, limit).ToList();
        }

        [Benchmark]
        public unsafe ParallelLoopResult ParallelRCWLookUp()
            => Parallel.ForEach(iterations, static (nint ptr) => QueryRCW(ptr));

        sealed class DummyComWrappers : ComWrappers
        {
            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
                => throw new NotImplementedException();

            protected override object CreateObject(nint externalComObject, CreateObjectFlags flags)
            {
                Marshal.AddRef(externalComObject);
                return new object();
            }

            protected override void ReleaseObjects(IEnumerable objects)
                => throw new NotImplementedException();
        }

        static Guid IID_IUnknown = new Guid(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);

        [UnmanagedCallersOnly]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvMemberFunction) })]
        static int QueryInterface(void* _this, Guid* iid, void** ppObj)
        {
            if (!IID_IUnknown.Equals(*iid))
            {
                return unchecked((int)0x80004002 /* E_NOINTERFACE */);
            }

            *ppObj = _this;
            AddRef2(_this);
            return 0 /* S_OK */;
        }

        static int AddRef2(void* _this)
        {
            var obj = (void**)_this;
            return Interlocked.Increment(ref Unsafe.AsRef<int>((int*)&obj[1]));
        }

        [UnmanagedCallersOnly]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvMemberFunction) })]
        static int AddRef(void* _this)
            => AddRef2(_this);

        [UnmanagedCallersOnly]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvMemberFunction) })]
        static int Release(void* _this)
        {
            var obj = (void**)_this;
            int r = Interlocked.Decrement(ref Unsafe.AsRef<int>((int*)&obj[1]));
            if (r == 0)
            {
                NativeMemory.Free(_this);
            }
            return r;
        }
    }
}