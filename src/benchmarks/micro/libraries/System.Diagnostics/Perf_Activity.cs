// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using MicroBenchmarks;

namespace System.Diagnostics
{
    [MemoryDiagnoser]
    [BenchmarkCategory(Categories.Libraries)]
    public class Perf_Activity
    {
        [Params(ActivityIdFormat.Hierarchical, ActivityIdFormat.W3C)]
        public ActivityIdFormat IdFormat { get; set; }

        [Benchmark]
        public void ActivityAllocations()
        {
            Activity activity = new Activity("TestActivity");
            activity.SetIdFormat(IdFormat);
            activity.Start();
            activity.Stop();
        }
    }
}
