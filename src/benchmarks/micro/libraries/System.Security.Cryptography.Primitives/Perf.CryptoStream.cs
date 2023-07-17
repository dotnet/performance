// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace System.Security.Cryptography.Primitives.Tests.Performance
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public class Perf_CryptoStream
    {
        private readonly byte[] _data = Enumerable.Range(0, 10_000_000).Select(i => (byte)i).ToArray();
        private readonly MemoryStream _destination = new MemoryStream();

        [Benchmark]
        [MemoryRandomization]
        public async Task Base64EncodeAsync()
        {
            _destination.Position = 0;
            using (var toBase64 = new ToBase64Transform())
            using (var stream = new CryptoStream(_destination, toBase64, CryptoStreamMode.Write, leaveOpen: true))
            {
                await stream.WriteAsync(_data, 0, _data.Length);
            }
        }
    }
}
