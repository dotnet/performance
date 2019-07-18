// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using Microsoft.Win32.SafeHandles;

namespace System.Runtime.InteropServices.Tests
{
    [BenchmarkCategory(Categories.CoreFX)]
    public class SafeHandleTests
    {
        private readonly SafeFileHandle _sfh = new SafeFileHandle((IntPtr)12345, ownsHandle: false);

        [Benchmark]
        public IntPtr AddRef_GetHandle_Release()
        {
            bool success = false;
            try
            {
                _sfh.DangerousAddRef(ref success);
                return _sfh.DangerousGetHandle();
            }
            finally
            {
                if (success)
                {
                    _sfh.DangerousRelease();
                }
            }
        }

    }
}
