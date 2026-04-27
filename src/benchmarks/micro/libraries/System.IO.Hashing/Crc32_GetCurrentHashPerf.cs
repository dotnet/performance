// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.IO.Hashing.Tests
{
    public class Crc32_GetCurrentHashPerf : Crc_GetCurrentHashPerf<Crc32>
    {
#if NET8_0_OR_GREATER
        [Benchmark]
        public uint GetCurrentHashAsUInt32()
        {
            return Crc.GetCurrentHashAsUInt32();
        }
#endif
    }
}
