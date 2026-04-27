// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Tests
{
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Environment
    {
        private const string Key = "7efd538f-dcab-4806-839a-972bc463a90c";
        private const string ExpandedKey = "%" + Key + "%";
        
        [GlobalSetup]
        public void Setup() => Environment.SetEnvironmentVariable(Key, "value");

        [GlobalCleanup]
        public void Cleanup() => Environment.SetEnvironmentVariable(Key, null);
        
        [Benchmark]
        public string GetEnvironmentVariable() => Environment.GetEnvironmentVariable(Key);

        [Benchmark]
        public string ExpandEnvironmentVariables() => Environment.ExpandEnvironmentVariables(ExpandedKey);

        [Benchmark]
        public IDictionary GetEnvironmentVariables() => Environment.GetEnvironmentVariables();

        [Benchmark]
        [Arguments(Environment.SpecialFolder.System, Environment.SpecialFolderOption.None)]
        [MemoryRandomization]
        public void GetFolderPath(Environment.SpecialFolder folder, Environment.SpecialFolderOption option)
            => Environment.GetFolderPath(folder, option);

        [Benchmark]
        [BenchmarkCategory(Categories.NoAOT)]
        public string[] GetLogicalDrives() => Environment.GetLogicalDrives();

        [Benchmark(OperationsPerInvoke = 2)]
        public void SetEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable(Key, "some value 1");
            Environment.SetEnvironmentVariable(Key, "some value 2");
        }
    }
}
