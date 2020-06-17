// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.IO.Pipes.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public abstract class Perf_PipeTest : PipeTestBase
    {
        [Params(1000000)]
        public int size; 

        private byte[] _sent;
        private byte[] _received;
        private ServerClientPair _serverClientPair;

        [GlobalSetup]
        public void Setup()
        {
            Random rand = new Random(314);
            _sent = new byte[size];
            _received = new byte[size];
            rand.NextBytes(_sent);
            
            _serverClientPair = CreateServerClientPair();
        }
        
        [Benchmark]
        public async Task ReadWrite()
        {
            Task write = Task.Run(() => _serverClientPair.writeablePipe.Write(_sent, 0, _sent.Length));
            int totalReadLength = 0;
            while (totalReadLength < _sent.Length)
            {
                int readLength = _serverClientPair.readablePipe.Read(_received, totalReadLength, size - totalReadLength);
                totalReadLength += readLength;
            }

            await write;
        }
    }
}
