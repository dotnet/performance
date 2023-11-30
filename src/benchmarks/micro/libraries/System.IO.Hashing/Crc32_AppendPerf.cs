// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.IO.Hashing.Tests
{
    public class Crc32_AppendPerf : Crc_AppendPerf<Crc32>
    {
        [Params(16, 128, 10240)]
        public override int BufferSize { get; set; }
    }
}
