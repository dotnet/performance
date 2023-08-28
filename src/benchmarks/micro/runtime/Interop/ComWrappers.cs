// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Interop
{
    [BenchmarkCategory(Categories.Runtime, Categories.NoWASM, Categories.NoMono)]
    public class ComWrappersTests
    {
        static readonly ComWrappers s_instance = new DummyComWrappers();

        private nint _ptr;
        private object _targetRcw;

        public ComWrappersTests()
        {
            // Let the ComWrappers API create a valid IUnknown instance.
            // This can then be passed as an unmanaged instance since ComWrappers
            // doesn't unwrap by default.
            object tmp = new object();
            _ptr = s_instance.GetOrCreateComInterfaceForObject(tmp, CreateComInterfaceFlags.None);

            // Transfer ownership to RCW instance. This populates the RCW cache.
            _targetRcw = s_instance.GetOrCreateObjectForComInstance(_ptr, CreateObjectFlags.None);
            if (tmp == _targetRcw)
            {
                throw new Exception("Test invariant violated. Assuming roundtrip for CCW to RCW doesn't unwrap.");
            }

            // Ownership was transferred.
            Marshal.Release(_ptr);
        }

        [Benchmark]
        public async Task ParallelRCWLookUp()
        {
            await Task.WhenAll(
                Enumerable.Range(0, Environment.ProcessorCount)
                    .Select(_ =>
                        Task.Run(delegate
                        {
                            // Define a large number of iterations for parallel action.
                            for (int i = 0; i < 3_000_000; i++)
                            {
                                s_instance.GetOrCreateObjectForComInstance(_ptr, CreateObjectFlags.None);
                            }
                        })));
        }

        sealed class DummyComWrappers : ComWrappers
        {
            protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 0;
                return null;
            }

            protected override object CreateObject(nint externalComObject, CreateObjectFlags flags)
            {
                Marshal.AddRef(externalComObject);
                return new object();
            }

            protected override void ReleaseObjects(IEnumerable objects)
                => throw new NotImplementedException();
        }
    }
}