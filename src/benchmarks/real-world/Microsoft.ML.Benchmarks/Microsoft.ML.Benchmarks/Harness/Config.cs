// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Microsoft.ML.Benchmarks
{
    public class RecommendedConfig : ManualConfig
    {
        public RecommendedConfig()
        {
            Add(DefaultConfig.Instance); // this config contains all of the basic settings (exporters, columns etc)

            Add(GetJobDefinition());// job defines how many times given benchmark should be executed
        }

        protected virtual Job GetJobDefinition()
            => Job.Default
                .WithWarmupCount(1) // ML.NET benchmarks are typically CPU-heavy benchmarks, 1 warmup is usually enough
                .WithMaxIterationCount(20)
                .AsDefault(); // this way we tell BDN that it's a default config which can be overwritten
    }
}
