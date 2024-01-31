// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public unsafe class UnmanagedMemoryStreamTests
    {
        private const int Length = 10000;
        private byte* _buffer;

        [GlobalSetup]
        public void Setup()
        {
            _buffer = (byte*)Marshal.AllocCoTaskMem(Length);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Marshal.FreeCoTaskMem((IntPtr)_buffer);
        }

        [Benchmark]
        public void ReadAsBytes()
        {
            using (var ums = new UnmanagedMemoryStream(_buffer, Length))
            {
                while (ums.ReadByte() >= 0)
                {
                }
            }
        }

        [Benchmark]
        public void ReadAsArrays()
        {
            var array = new byte[128];
            using (var ums = new UnmanagedMemoryStream(_buffer, Length))
            {
                while (ums.Read(array, 0, array.Length) != 0)
                {
                }
            }
        }
    }
}
