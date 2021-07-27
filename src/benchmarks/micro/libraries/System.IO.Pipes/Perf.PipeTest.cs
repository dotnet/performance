// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.IO.Pipes.Tests
{
    [BenchmarkCategory(Categories.Libraries, Categories.NoWASM)]
    public abstract class Perf_PipeTest : PipeTestBase
    {
        private const int Iterations = 1000;

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

        [GlobalCleanup]
        public void Cleanup() => _serverClientPair.Dispose();
        
        [Benchmark(OperationsPerInvoke = Iterations)]
        public async Task ReadWrite()
        {
            Task write = Task.Run(delegate
            {
                for (int i = 0; i < Iterations; i++)
                {
                    _serverClientPair.writeablePipe.Write(_sent, 0, _sent.Length);
                }
            });

            for (int i = 0; i < Iterations; i++)
            {
                int totalReadLength = 0;
                while (totalReadLength < _sent.Length)
                {
                    totalReadLength += _serverClientPair.readablePipe.Read(_received, totalReadLength, size - totalReadLength);
                }
            }

            await write;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public async Task ReadWriteAsync()
        {
            Task write = Task.Run(async delegate
            {
                for (int i = 0; i < Iterations; i++)
                {
#if !NETFRAMEWORK
                    await _serverClientPair.writeablePipe.WriteAsync(_sent);
#else
                    await _serverClientPair.writeablePipe.WriteAsync(_sent, 0, _sent.Length);
#endif
                }
            });

            for (int i = 0; i < Iterations; i++)
            {
                int totalReadLength = 0;
                while (totalReadLength < _sent.Length)
                {
                    totalReadLength += await
#if !NETFRAMEWORK
                        _serverClientPair.readablePipe.ReadAsync(_received.AsMemory(totalReadLength));
#else
                        _serverClientPair.readablePipe.ReadAsync(_received, totalReadLength, size - totalReadLength);
#endif
                }
            }

            await write;
        }
    }
}
