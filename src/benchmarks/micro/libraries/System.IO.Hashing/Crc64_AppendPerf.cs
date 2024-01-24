// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace System.IO.Hashing.Tests
{
    public class Crc64_AppendPerf : Crc_AppendPerf<Crc64>
    {
        [Params(16, 256, 10240)]
        public override int BufferSize { get; set; }
    }
}
